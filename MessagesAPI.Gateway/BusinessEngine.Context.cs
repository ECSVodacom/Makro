﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MessagesAPI.Gateway
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class BusinessEngine : DbContext
    {
        public BusinessEngine()
            : base("name=BusinessEngine")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Error> Errors { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<StandardDocument> StandardDocuments { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<SupplierVendor> SupplierVendors { get; set; }
        public virtual DbSet<API> APIs { get; set; }
        public virtual DbSet<Status> Statuses { get; set; }
    
        public virtual ObjectResult<string> GetSellerGln(string orderNumber, string supplierEan)
        {
            var orderNumberParameter = orderNumber != null ?
                new ObjectParameter("OrderNumber", orderNumber) :
                new ObjectParameter("OrderNumber", typeof(string));
    
            var supplierEanParameter = supplierEan != null ?
                new ObjectParameter("SupplierEan", supplierEan) :
                new ObjectParameter("SupplierEan", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<string>("GetSellerGln", orderNumberParameter, supplierEanParameter);
        }
    }
}
