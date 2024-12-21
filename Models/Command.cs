using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemRemoteService.Models
{
    public class Command
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
