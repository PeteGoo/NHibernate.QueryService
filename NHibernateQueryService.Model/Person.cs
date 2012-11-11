using System.Collections.Generic;
using System.Data.Services.Common;
using Iesi.Collections.Generic;

namespace NHibernateQueryService.Model {
    [DataServiceKey("Id")] 
    public class Person {
        private ICollection<Address> addresses = new HashedSet<Address>();

        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }

        public virtual ICollection<Address> Addresses {
            get { return addresses; }
        }
    }

}