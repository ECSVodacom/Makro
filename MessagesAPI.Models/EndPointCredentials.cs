using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesAPI.Models
{
    public class PostData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EndPoint { get; set; }
        public string NameSpace { get; set; }
        public string Gln { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SellerGln { get; set; }
    }
}
