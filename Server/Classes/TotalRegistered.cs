using SQLite;

namespace Database.Tables
{
    [Table("Total")]
    public class TotalRegisetered
    {
        [PrimaryKey]
        [Column("TotalAccounts")]
        public int TotalAccounts {get; set;}
        [Column("ID")]
        public int ID {get; set;}
    }
}
