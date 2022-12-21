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
using zcfux.Data;
using zcfux.Translation.Data;

namespace zcfux.Translation.Test;

public abstract class ATranslationDbTests
{
    IEngine _engine = default!;
    ITranslationDb _translationDb = default!;


    [SetUp]
    public void Setup()
    {
        _engine = CreateAndSetupEngine();
        _translationDb = CreateTranslationDb();
    }

    [Test]
    public void WriteCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);
        }
    }

    [Test]
    public void GetCategories()
    {
        using (var t = _engine.NewTransaction())
        {
            var categories = _translationDb
                .GetCategories(t.Handle)
                .ToArray();

            Assert.IsEmpty(categories);

            _translationDb.WriteCategory(t.Handle, new Category(1, "foo"));
            _translationDb.WriteCategory(t.Handle, new Category(2, "bar"));

            categories = _translationDb
                .GetCategories(t.Handle)
                .ToArray();

            Assert.AreEqual(2, categories.Length);

            Assert.IsTrue(categories.Any(c => c is { Id: 1, Name: "foo" }));
            Assert.IsTrue(categories.Any(c => c is { Id: 2, Name: "bar" }));
        }
    }


    [Test]
    public void DeleteCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            category = new Category(2, "bar");

            _translationDb.WriteCategory(t.Handle, category);

            _translationDb.DeleteCategory(t.Handle, 1);

            var categories = _translationDb
                .GetCategories(t.Handle)
                .ToArray();

            Assert.AreEqual(1, categories.Length);

            Assert.IsTrue(categories.Any(c => c is { Id: 2, Name: "bar" }));
        }
    }

    [Test]
    public void DeleteNonExistingCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() =>
            {
                _translationDb.DeleteCategory(t.Handle, TestContext.CurrentContext.Random.Next());
            });
        }
    }

    [Test]
    public void WriteLocale()
    {
        using (var t = _engine.NewTransaction())
        {
            var locale = new Locale(1, "foo");

            _translationDb.WriteLocale(t.Handle, locale);
        }
    }

    [Test]
    public void GetLocales()
    {
        using (var t = _engine.NewTransaction())
        {
            var locales = _translationDb
                .GetLocales(t.Handle)
                .ToArray();

            Assert.IsEmpty(locales);

            _translationDb.WriteLocale(t.Handle, new Locale(1, "foo"));
            _translationDb.WriteLocale(t.Handle, new Locale(2, "bar"));

            locales = _translationDb
                .GetLocales(t.Handle)
                .ToArray();

            Assert.AreEqual(2, locales.Length);

            Assert.IsTrue(locales.Any(c => c is { Id: 1, Name: "foo" }));
            Assert.IsTrue(locales.Any(c => c is { Id: 2, Name: "bar" }));
        }
    }

    [Test]
    public void DeleteLocale()
    {
        using (var t = _engine.NewTransaction())
        {
            var locale = new Locale(1, "foo");

            _translationDb.WriteLocale(t.Handle, locale);

            locale = new Locale(2, "bar");

            _translationDb.WriteLocale(t.Handle, locale);

            _translationDb.DeleteLocale(t.Handle, 1);

            var locales = _translationDb
                .GetLocales(t.Handle)
                .ToArray();

            Assert.AreEqual(1, locales.Length);

            Assert.IsTrue(locales.Any(c => c is { Id: 2, Name: "bar" }));
        }
    }

    [Test]
    public void DeleteNonExistingLocale()
    {
        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() =>
            {
                _translationDb.DeleteLocale(t.Handle, TestContext.CurrentContext.Random.Next());
            });
        }
    }

    [Test]
    public void NewTextResource()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            Assert.IsInstanceOf<ITextResource>(resource);

            Assert.GreaterOrEqual(resource.Id, 1);
            Assert.AreEqual(1, resource.Category.Id);
            Assert.AreEqual("foo", resource.Category.Name);
            Assert.AreEqual("hello world", resource.MsgId);
        }
    }

    [Test]
    public void NewTextResourceWithoutCategory()
    {
        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() =>
            {
                _translationDb.NewTextResource(t.Handle, new Category(1, "foo"), "hello world");
            });
        }
    }

    [Test]
    public void TryGetTextResource()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.TryGetTextResource(t.Handle, category, "hello world");

            Assert.IsNull(resource);

            resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            Assert.IsInstanceOf<ITextResource>(resource);

            Assert.GreaterOrEqual(resource.Id, 1);
            Assert.AreEqual(1, resource.Category.Id);
            Assert.AreEqual("foo", resource.Category.Name);
            Assert.AreEqual("hello world", resource.MsgId);
        }
    }

    [Test]
    public void GetOrCreateTextResource()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            var first = _translationDb.GetOrCreateTextResource(t.Handle, category, "hello world");

            Assert.GreaterOrEqual(first.Id, 1);
            Assert.AreEqual(1, first.Category.Id);
            Assert.AreEqual("foo", first.Category.Name);
            Assert.AreEqual("hello world", first.MsgId);

            var second = _translationDb.GetOrCreateTextResource(t.Handle, category, "hello world");

            Assert.GreaterOrEqual(first.Id, second.Id);
            Assert.AreEqual(first.Category.Id, second.Category.Id);
            Assert.AreEqual(first.Category.Name, second.Category.Name);
            Assert.AreEqual(first.MsgId, second.MsgId);
        }
    }

    [Test]
    public void DeleteCategoryDeletesReferences()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            _translationDb.NewTextResource(t.Handle, category, "hello world");

            _translationDb.DeleteCategory(t.Handle, category.Id);

            var resource = _translationDb.TryGetTextResource(t.Handle, category, "hello world");

            Assert.IsNull(resource);
        }
    }

    [Test]
    public void DeleteTextResource()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            var hello = _translationDb.NewTextResource(t.Handle, category, "hello");
            var world = _translationDb.NewTextResource(t.Handle, category, "world");

            _translationDb.DeleteTextResource(t.Handle, world.Id);

            var received = _translationDb.TryGetTextResource(t.Handle, category, "hello")!;

            Assert.AreEqual(hello.Id, received.Id);
            Assert.AreEqual(hello.Category.Id, received.Category.Id);
            Assert.AreEqual(hello.Category.Name, received.Category.Name);
            Assert.AreEqual(hello.MsgId, received.MsgId);

            received = _translationDb.TryGetTextResource(t.Handle, category, "world");

            Assert.IsNull(received);
        }
    }

    [Test]
    public void DeleteNonExistingTextResource()
    {
        using (var t = _engine.NewTransaction())
        {
            Assert.Throws<NotFoundException>(() =>
            {
                _translationDb.DeleteTextResource(t.Handle, TestContext.CurrentContext.Random.Next());
            });
        }
    }

    [Test]
    public void LocalizeText()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "ui");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            var locale = new Locale(1, "de-DE");

            _translationDb.WriteLocale(t.Handle, locale);

            _translationDb.LocalizeText(t.Handle, resource, locale, "hallo welt");
        }
    }

    [Test]
    public void LocalizeTextWithoutLocale()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "ui");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            var locale = new Locale(1, "de-DE");

            Assert.Throws<NotFoundException>(() =>
            {
                _translationDb.LocalizeText(t.Handle, resource, locale, "hallo welt");
            });
        }
    }


    [Test]
    public void DeleteResourceDeletesReferences()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "ui");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            var locale = new Locale(1, "de-DE");

            _translationDb.WriteLocale(t.Handle, locale);

            _translationDb.LocalizeText(t.Handle, resource, locale, "hallo welt");

            _translationDb.DeleteTextResource(t.Handle, resource.Id);

            var translation = _translationDb.GetTranslation(t.Handle, "de-DE");

            Assert.IsEmpty(translation);
        }
    }

    [Test]
    public void DeleteLocaleDeletesReferences()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "ui");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            var locale = new Locale(1, "de-DE");

            _translationDb.WriteLocale(t.Handle, locale);

            _translationDb.LocalizeText(t.Handle, resource, locale, "hallo welt");

            _translationDb.DeleteLocale(t.Handle, locale.Id);

            var translation = _translationDb.GetTranslation(t.Handle, "de-DE");

            Assert.IsEmpty(translation);
        }
    }

    [Test]
    public void GetTranslation()
    {
        using (var t = _engine.NewTransaction())
        {
            var category = new Category(1, "foo");

            _translationDb.WriteCategory(t.Handle, category);

            var resource = _translationDb.NewTextResource(t.Handle, category, "hello world");

            var deLocale = new Locale(1, "de-DE");

            _translationDb.WriteLocale(t.Handle, deLocale);

            _translationDb.LocalizeText(t.Handle, resource, deLocale, "hallo welt");

            var usLocale = new Locale(2, "en-US");

            _translationDb.WriteLocale(t.Handle, usLocale);

            _translationDb.LocalizeText(t.Handle, resource, usLocale, "hello world");

            var german = _translationDb
                .GetTranslation(t.Handle, "de-DE")
                .Single()
                .Value;

            Assert.AreEqual("hallo welt", german);

            var english = _translationDb
                .GetTranslation(t.Handle, "en-US")
                .Single()
                .Value;

            Assert.AreEqual("hello world", english);

            var notFound = _translationDb
                .GetTranslation(t.Handle, "hello world");

            Assert.IsEmpty(notFound);
        }
    }

    protected abstract IEngine CreateAndSetupEngine();

    protected abstract ITranslationDb CreateTranslationDb();
}