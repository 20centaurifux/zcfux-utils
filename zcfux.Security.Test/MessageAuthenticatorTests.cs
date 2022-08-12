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
using NUnit.Framework;
using zcfux.Byte;

namespace zcfux.Security.Test;

public sealed class MessageAuthenticatorTests
{
    [Test]
    public void MessageTest()
    {
        var secret = TestContext.CurrentContext.Random.GetString();

        var auth = new MessageAuthenticator(MessageAuthenticator.DefaultAlgorithm, secret);

        var message = TestContext.CurrentContext.Random.GetString();

        var hash = auth.ComputeHash(message);
        
        Assert.AreEqual(hash.Algorithm, MessageAuthenticator.DefaultAlgorithm);

        Assert.AreEqual(hash.Digest, auth.ComputeHash(message.GetBytes()).Digest);
        
        Assert.IsTrue(auth.Verify(message, hash.Digest));
        Assert.IsTrue(auth.Verify(message.GetBytes(), hash.Digest));
        
        var newMessage = TestContext.CurrentContext.Random.GetString();

        Assert.IsFalse(auth.Verify(newMessage, hash.Digest));
        Assert.IsFalse(auth.Verify(newMessage.GetBytes(), hash.Digest));

        var newHash = SecureRandom.GetBytes(hash.Digest.Length);

        Assert.IsFalse(auth.Verify(message, newHash));
        Assert.IsFalse(auth.Verify(message.GetBytes(), newHash));
    }

    [Test]
    public void WrongSecretTest()
    {
        var secret = TestContext.CurrentContext.Random.GetString();

        var auth = new MessageAuthenticator(secret);

        var message = TestContext.CurrentContext.Random.GetString();

        var hash = auth.ComputeHash(message);

        var newSecret = TestContext.CurrentContext.Random.GetString();

        var newAuth = new MessageAuthenticator(newSecret);

        Assert.IsFalse(newAuth.Verify(message, hash.Digest));
    }
    
    [Test]
    public void WrongAlgorithmTest()
    {
        var secret = TestContext.CurrentContext.Random.GetString();

        var auth = new MessageAuthenticator("HMAC-SHA-256", secret);

        var message = TestContext.CurrentContext.Random.GetString();

        var hash = auth.ComputeHash(message);

        var newAuth = new MessageAuthenticator("HMAC-SHA-384", secret);

        Assert.IsFalse(newAuth.Verify(message, hash.Digest));
    }
}