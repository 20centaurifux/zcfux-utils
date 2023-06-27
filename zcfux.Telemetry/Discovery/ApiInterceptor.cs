/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Reflection;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Internal;

namespace zcfux.Telemetry.Discovery;

sealed class ApiInterceptor : IInterceptor
{
    sealed record Command(string Topic, uint TimeToLive, uint ResponseTimeout);

    sealed record Event(IProducer Producer, Type ParameterType);

    readonly Type _apiType;
    readonly string _apiTopic;

    readonly IDictionary<string, Command> _commands;
    readonly IDictionary<string, Event> _events;
    readonly IDictionary<string, Event> _eventGetters;

    event Func<CommandEventArgs, Task>? SendCommandAsync;
    event Func<RequestEventArgs, Task>? SendRequestAsync;

    public ApiInterceptor(Type type)
    {
        _apiType = type;

        var attr = _apiType
            .GetCustomAttributes(typeof(ApiAttribute), false)
            .OfType<ApiAttribute>()
            .Single();

        _apiTopic = attr.Topic;

        _commands = new Dictionary<string, Command>(DiscoverCommands());

        var events = DiscoverEvents().ToArray();

        _events = events.ToDictionary(t => t.Topic, t => t.Event);
        _eventGetters = events.ToDictionary(t => t.PropertyName, t => t.Event);
    }

    IEnumerable<KeyValuePair<string, Command>> DiscoverCommands()
    {
        foreach (var itf in _apiType.GetAllInterfaces())
        {
            var methods = itf.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods)
            {
                if (method.GetCustomAttributes(typeof(CommandAttribute), false).SingleOrDefault() is CommandAttribute attr)
                {
                    if (method.ReturnType == typeof(void)
                        || method.ReturnType == typeof(Task)
                        || method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        yield return new KeyValuePair<string, Command>(
                            MethodSignature(method),
                            new Command(attr.Topic, attr.TimeToLive, attr.ResponseTimeout));
                    }
                }
            }
        }
    }

    IEnumerable<(string PropertyName, string Topic, Event Event)> DiscoverEvents()
    {
        foreach (var itf in _apiType.GetAllInterfaces())
        {
            var props = itf.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in props)
            {
                if (prop.GetCustomAttributes(typeof(EventAttribute), false).SingleOrDefault() is EventAttribute attr)
                {
                    if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                    {
                        var parameterType = prop
                            .PropertyType
                            .GetGenericArguments()
                            .Single();

                        var ctor = typeof(Producer<>)
                            .MakeGenericType(parameterType)
                            .GetConstructor(Array.Empty<Type>());

                        var producer = ctor!.Invoke(Array.Empty<object?>()) as IProducer;

                        var ev = new Event(producer!, parameterType);

                        yield return (prop.Name, attr.Topic, ev);
                    }
                }
            }
        }
    }

    public void Intercept(IInvocation invocation)
    {
        if (invocation.Method.Name.StartsWith("get_"))
        {
            InterceptGetter(invocation);
        }
        else if (invocation.Method.Name.StartsWith("add_"))
        {
            AddEventHandler(invocation);
        }
        else if (invocation.Method.Name.StartsWith("remove_"))
        {
            RemoveEventHandler(invocation);
        }
        else if (invocation.Method.Name.Equals("ReceiveEvent"))
        {
            ReceiveEvent(invocation);
        }
        else
        {
            InterceptCommand(invocation);
        }
    }

    void InterceptGetter(IInvocation invocation)
    {
        var propertyName = invocation.Method.Name[4..];

        if (propertyName == "EventTopics")
        {
            invocation.ReturnValue = _events
                .Select(ev => (ev.Key, ev.Value.ParameterType))
                .ToArray();
        }
        else
        {
            invocation.ReturnValue = _eventGetters[propertyName].Producer;
        }
    }

    void AddEventHandler(IInvocation invocation)
    {
        var eventName = invocation.Method.Name[4..];

        if (eventName == "SendCommandAsync")
        {
            SendCommandAsync += (Func<CommandEventArgs, Task>)invocation.Arguments[0];
        }
        else if (eventName == "SendRequestAsync")
        {
            SendRequestAsync += (Func<RequestEventArgs, Task>)invocation.Arguments[0];
        }
        else
        {
            throw new ArgumentException($"Event `{eventName}' not found.");
        }
    }

    void RemoveEventHandler(IInvocation invocation)
    {
        var eventName = invocation.Method.Name[6..];

        if (eventName == "SendCommandAsync")
        {
            SendCommandAsync -= (Func<CommandEventArgs, Task>)invocation.Arguments[0];
        }
        else if (eventName == "SendRequestAsync")
        {
            SendRequestAsync -= (Func<RequestEventArgs, Task>)invocation.Arguments[0];
        }
        else
        {
            throw new ArgumentException($"Event `{eventName}' not found.");
        }
    }

    void ReceiveEvent(IInvocation invocation)
    {
        var topic = invocation.Arguments[0] as string;

        if (_events.TryGetValue(topic!, out var ev))
        {
            ev.Producer.Write(invocation.Arguments[1]);
        }
    }

    void InterceptCommand(IInvocation invocation)
    {
        object? returnValue;

        var signature = MethodSignature(invocation.Method);

        if (_commands.TryGetValue(signature, out var command))
        {
            var parameter = invocation.Arguments.SingleOrDefault();

            if (invocation.Method.ReturnType == typeof(void)
                || invocation.Method.ReturnType == typeof(Task))
            {
                returnValue = SendCommandAsync?.Invoke(
                    new CommandEventArgs(
                        _apiTopic,
                        command.Topic,
                        command.TimeToLive,
                        parameter));
            }
            else
            {
                var genericType = invocation
                    .Method
                    .ReturnType
                    .GetGenericArguments()
                    .Single();

                var task = SendRequestAsync?.Invoke(
                    new RequestEventArgs(
                        _apiTopic,
                        command.Topic,
                        command.TimeToLive,
                        parameter,
                        MessageIdGenerator.Next(),
                        genericType,
                        command.ResponseTimeout));

                var awaitResponse = GetType()
                    .GetMethod("AwaitResponseAsync", BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(genericType);

                returnValue = awaitResponse.Invoke(
                    null,
                    new object?[]
                    {
                        task,
                        TimeSpan.FromSeconds(command.ResponseTimeout)
                    });
            }
        }
        else
        {
            throw new InvalidOperationException($"Method with signature `{signature}' not found.");
        }

        if (invocation.Method.ReturnType == typeof(void))
        {
            (returnValue as IAsyncResult)!.AsyncWaitHandle.WaitOne();
        }
        else
        {
            invocation.ReturnValue = returnValue;
        }
    }

    static Task<T> AwaitResponseAsync<T>(Task<object> task, TimeSpan responseTime)
        => Task.Run(async () =>
        {
            var result = await task.WaitAsync(responseTime);

            return (T)result;
        });

    static string MethodSignature(MethodInfo method)
    {
        var parameterType = method
            .GetParameters()
            .SingleOrDefault()
            ?.Name;

        return $"{method.ReturnType} {method.Name}({parameterType ?? string.Empty})";
    }
}