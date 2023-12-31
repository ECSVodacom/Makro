//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class Supplier
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Supplier()
        {
            this.StandardDocuments = new HashSet<StandardDocument>();
            this.SupplierVendors = new HashSet<SupplierVendor>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string AccountKey { get; set; }
        public string Ean { get; set; }
        public bool IsEnabled { get; set; }
        public bool AcceptRevisedAllocationOrders { get; set; }
        public bool AcceptRevisedNormalOrders { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FileNamePrefix { get; set; }
        public Nullable<int> API_Id { get; set; }
        public Nullable<bool> UseAccountKey { get; set; }
        public string SendEmailAddress { get; set; }
        public Nullable<bool> DoSendEmails { get; set; }
        public Nullable<bool> DoSendPdfs { get; set; }
    
        public virtual API API { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StandardDocument> StandardDocuments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SupplierVendor> SupplierVendors { get; set; }
    }
}
