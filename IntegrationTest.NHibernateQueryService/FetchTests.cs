using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Linq;
using NHibernateQueryService.Model;

namespace IntegrationTest.NHibernateQueryService {
    [TestClass]
    public class FetchTests {

        [TestMethod]
        public void SimplePerson() {
            using(var session = NHibernateHelper.OpenSession()) {
                var people = from person in session.Query<Person>()
                             where person.FirstName == "Peter"
                             select person;
                Assert.AreEqual(2, people.Count());
            }
        }

        [TestMethod]
        public void CanNavigateLazily() {
            IEnumerable<Person> people;
            using (var session = NHibernateHelper.OpenSession()) {
                people = from person in session.Query<Person>()
                         where person.FirstName == "Peter"
                         select person;
                Assert.AreEqual(2, people.Count());
                Assert.AreEqual(1, people.First().Addresses.Count);
            }
        }

        [TestMethod]
        public void CanIncludeEagerly() {
            IEnumerable<Person> people;
            using (var session = NHibernateHelper.OpenSession()) {
                people = (from person in session.Query<Person>().Fetch(p => p.Addresses)
                         where person.FirstName == "Peter"
                          select person).ToList();
                Assert.AreEqual(2, people.Count());
            }

            Assert.AreEqual(1, people.First().Addresses.Count);
        }

        [TestMethod]
        [Ignore]
        public void CanIncludeEagerlyUsingStringPaths() {
            IEnumerable<Person> people;
            using (var session = NHibernateHelper.OpenSession()) {
                people = (from person in session.Query<Person>().Include("Addresses")
                          where person.FirstName == "Peter"
                          select person).ToList();
                Assert.AreEqual(2, people.Count());
            }

            Assert.AreEqual(1, people.First().Addresses.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void AddressesShouldNotBeIncludedByDefault() {
            IEnumerable<Person> people;
            using (var session = NHibernateHelper.OpenSession()) {
                people = from person in session.Query<Person>()
                             where person.FirstName == "Peter"
                             select person;
                Assert.AreEqual(2, people.Count());
            }

            Assert.AreEqual(1, people.First().Addresses.Count);
        }

    }
}
