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
    
    public partial class StandardDocument
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StandardDocument()
        {
            this.Orders = new HashSet<Order>();
        }
    
        public long Id { get; set; }
        public string OrderNumbers { get; set; }
        public string StandardDocumentXml { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public int SupplierId { get; set; }
        public Nullable<bool> DoResend { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> Orders { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}
