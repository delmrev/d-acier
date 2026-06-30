using SQLite;
namespace Database.Tables
{
    [Table("UserStat")]
    public class UserStat
    {
        [PrimaryKey, AutoIncrement]
        public ulong Id { get; set; }
        [Indexed]
        [Column("eugen_id")]
        public long EugenID {get; set;}
        [Indexed]
        [Column("game_id")]
        public int GameID { get; set; }
        [Indexed]
        [Column("Key")]
        public string Key {get; set;}
        [Column("Value")]
        public int Value {get; set;}
    }
}