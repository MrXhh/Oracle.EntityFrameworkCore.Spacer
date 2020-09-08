using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp_EFCore_Oracle
{
    [Table("XhhTest")]
    public class xhhTest
    {
        [Key]
        public long Id { get; set; }

        public string Txt { get; set; }

    }

}