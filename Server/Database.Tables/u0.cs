using EugnetProtocol.Common.Interfaces;
using SQLite;

namespace Database.Tables
{
    [Table("u0")]
    public class u0
    {
        [PrimaryKey,AutoIncrement]
        [Column("EugenID")]
        public long EugenID {get; set;}
        [Indexed]
        [Column("SteamID")]
        public long SteamID {get; set;}
        [Column("rev")]
        public string? Rev {get; set;}
        [Column("name")]
        public string? Name {get; set;}
        [Column("avatar")]
        public string? Avatar {get; set;}
    }
}
