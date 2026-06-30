using SQLite;

namespace Database.Tables
{
    [Table("ClientInfo")]
    public class ClientInfo
    {
        [PrimaryKey]
        [Column("eugen_id")]
        public long EugenID {get; set;}
        [Column("Login")]
        public string? Login {get; set;}
        [Column("Password")]
        public string? Password {get; set;}
    }
}