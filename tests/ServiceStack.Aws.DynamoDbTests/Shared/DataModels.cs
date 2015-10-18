using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDbTests.Shared
{
    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        public CustomerAddress PrimaryAddress { get; set; }

        public List<Order> Orders { get; set; }

        protected bool Equals(Customer other)
        {
            return Id == other.Id &&
                string.Equals(Name, other.Name) &&
                Equals(PrimaryAddress, other.PrimaryAddress) &&
                Orders.EquivalentTo(other.Orders);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Customer)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PrimaryAddress != null ? PrimaryAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Orders != null ? Orders.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class CustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }

        protected bool Equals(CustomerAddress other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(AddressLine1, other.AddressLine1) &&
                string.Equals(AddressLine2, other.AddressLine2) &&
                string.Equals(City, other.City) &&
                string.Equals(State, other.State) &&
                string.Equals(Country, other.Country);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerAddress)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (AddressLine1 != null ? AddressLine1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AddressLine2 != null ? AddressLine2.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Customer))]
        public int CustomerId { get; set; }

        public string LineItem { get; set; }

        public int Qty { get; set; }

        public virtual decimal Cost { get; set; }

        protected bool Equals(Order other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(LineItem, other.LineItem) &&
                Qty == other.Qty &&
                Cost == other.Cost;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Order)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (LineItem != null ? LineItem.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Qty;
                hashCode = (hashCode * 397) ^ Cost.GetHashCode();
                return hashCode;
            }
        }
    }

    public class OrderWithFieldIndex : Order
    {
        [Index]
        public override decimal Cost { get; set; }
    }

    [Alias("CustomCostIndex")]
    public class OrderCostIndex : ILocalIndex<OrderWithLocalTypedIndex>
    {
        public int CustomerId { get; set; }
        [Index]
        public decimal Cost { get; set; }
        public int Id { get; set; }
        public int Qty { get; set; }
    }

    [References(typeof(OrderCostIndex))]
    public class OrderWithLocalTypedIndex : Order { }

    [CompositeKey("ProductId", "Cost")]
    public class OrderGlobalCostIndex : IGlobalIndex<OrderWithGlobalTypedIndex>
    {
        public int ProductId { get; set; }
        public decimal Cost { get; set; }
        public int Qty { get; set; }
        public int Id { get; set; }
    }

    [References(typeof(OrderGlobalCostIndex))]
    public class OrderWithGlobalTypedIndex : Order
    {
        public int ProductId { get; set; }
    }

    public class Country
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }

        protected bool Equals(Country other)
        {
            return Id == other.Id
                && string.Equals(CountryName, other.CountryName)
                && string.Equals(CountryCode, other.CountryCode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Country)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CountryName != null ? CountryName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CountryCode != null ? CountryCode.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Node> Children { get; set; }

        public Node() { }

        public Node(int id, string name, IEnumerable<Node> children = null)
        {
            Id = id;
            Name = name;
            if (children != null)
                Children = children.ToList();
        }

        protected bool Equals(Node other)
        {
            return Id == other.Id &&
                string.Equals(Name, other.Name) &&
                Children.EquivalentTo(other.Children);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Children != null ? Children.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Poco
    {
        public int Id { get; set; }

        public string Title { get; set; }

        protected bool Equals(Poco other)
        {
            return Id == other.Id && string.Equals(Title, other.Title);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Poco)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Title != null ? Title.GetHashCode() : 0);
            }
        }
    }

    public class Collection
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string[] ArrayStrings { get; set; }
        public HashSet<string> SetStrings { get; set; }
        public List<string> ListStrings { get; set; }

        public int[] ArrayInts { get; set; }
        public HashSet<int> SetInts { get; set; }
        public List<int> ListInts { get; set; }

        public Dictionary<int, int> DictionaryInts { get; set; }
        public Dictionary<string, string> DictionaryStrings { get; set; }

        public Collection InitStrings(params string[] strings)
        {
            ArrayStrings = strings;
            SetStrings = new HashSet<string>(strings);
            ListStrings = new List<string>(strings);
            DictionaryStrings = new Dictionary<string, string>();
            strings.Each(x => DictionaryStrings[x] = x);
            return this;
        }

        public Collection InitInts(params int[] ints)
        {
            ArrayInts = ints;
            SetInts = new HashSet<int>(ints);
            ListInts = new List<int>(ints);
            DictionaryInts = new Dictionary<int, int>();
            ints.Each(x => DictionaryInts[x] = x);
            return this;
        }

        protected bool Equals(Collection other)
        {
            return Id == other.Id
                && ArrayStrings.EquivalentTo(other.ArrayStrings)
                && SetStrings.EquivalentTo(other.SetStrings)
                && ListStrings.EquivalentTo(other.ListStrings)
                && ArrayInts.EquivalentTo(other.ArrayInts)
                && SetInts.EquivalentTo(other.SetInts)
                && ListInts.EquivalentTo(other.ListInts)
                && DictionaryInts.EquivalentTo(other.DictionaryInts)
                && DictionaryStrings.EquivalentTo(other.DictionaryStrings);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Collection)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (ArrayStrings != null ? ArrayStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SetStrings != null ? SetStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ListStrings != null ? ListStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ArrayInts != null ? ArrayInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SetInts != null ? SetInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ListInts != null ? ListInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DictionaryInts != null ? DictionaryInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DictionaryStrings != null ? DictionaryStrings.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class TableWithDynamoAttributes
    {
        public string A { get; set; }
        public string B { get; set; }

        [DynamoDBRangeKey]
        public string C { get; set; }

        [DynamoDBHashKey]
        public string D { get; set; }

        public string E { get; set; }
    }

    [CompositeKey("D", "C")]
    public class TableWithCompositeKey
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
    }

    public class GlobalIndexWithInterfaceAttrs : IGlobalIndex<TableWithTypedGlobalIndex>
    {
        public string A { get; set; }
        [PrimaryKey]
        public string B { get; set; }
        public string C { get; set; }
        [RangeKey]
        public string D { get; set; }
    }

    [References(typeof(GlobalIndexWithInterfaceAttrs))]
    public class TableWithTypedGlobalIndex
    {
        public string A { get; set; }
        public string B { get; set; }
        [RangeKey]
        public string C { get; set; }
        [HashKey]
        public string D { get; set; }
        public string E { get; set; }
    }

    public class TableWithConventionNames
    {
        public string A { get; set; }
        public string HashKey { get; set; }
        public string RangeKey { get; set; }
    }

    public class TableWithIdConvention
    {
        public string A { get; set; }
        public string Id { get; set; }
        public string RangeKey { get; set; }
    }

    [ProvisionedThroughput(ReadCapacityUnits = 100, WriteCapacityUnits = 50)]
    public class TableWithProvision
    {
        public string Id { get; set; }
        public string A { get; set; }
    }

    [ProvisionedThroughput(ReadCapacityUnits = 100, WriteCapacityUnits = 50)]
    public class GlobalIndexProvision : IGlobalIndex<TableWithGlobalIndexProvision>
    {
        [PrimaryKey]
        public string A { get; set; }
        [Index]
        public string Id { get; set; }
    }

    [References(typeof(GlobalIndexProvision))]
    public class TableWithGlobalIndexProvision
    {
        public string Id { get; set; }
        public string A { get; set; }
    }
}