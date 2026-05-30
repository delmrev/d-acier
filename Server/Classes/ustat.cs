using SQLite;

namespace Database.Tables
{
    [Table("Stat")]
    public class Stat
    {
        [PrimaryKey]
        [Column("eugen_id")]
        public long EugenID { get; set; }
        [Indexed]
        [Column("game_id")]
        public int GameID { get; set; }
        [Column("nb_dca_bought")]
        public int nb_dca_bought { get; set; }
        [Column("time_menu_played")]
        public int time_menu_played { get; set; }
        [Column("nb_air_bought")]
        public int nb_air_bought { get; set; }
        [Column("level")]
        public int level { get; set; }
        [Column("xp_campaign")]
        public int xp_campaign { get; set; }
        [Column("xp_skirmish")]
        public int xp_skirmish { get; set; }
        [Column("gametype_conquest")]
        public int gametype_conquest { get; set; }
        [Column("gametype_closequarter_conquest")]
        public int gametype_closequarter_conquest { get; set; }
        [Column("gametype_breakthrough")]
        public int gametype_breakthrough { get; set; }
        [Column("skirmish_played")]
        public int skirmish_played { get; set; }
        [Column("skirmish_nato")]
        public int skirmishNato { get; set; }
        [Column("skirmish_pact")]
        public int skirmish_pact { get; set; }
        [Column("skirmish_win")]
        public int skirmish_win { get; set; }
        [Column("skirmish_loss")]
        public int skirmish_loss { get; set; }
        [Column("skirmish_draw")]
        public int skirmish_draw { get; set; }
        [Column("skirmish_win_ai_1")]
        public int skirmish_win_ai_1 { get; set; }
        [Column("skirmish_win_ai_2")]
        public int skirmish_win_ai_2 { get; set; }
        [Column("skirmish_win_ai_3")]
        public int skirmish_win_ai_3 { get; set; }
        [Column("skirmish_win_ai_4")]
        public int skirmish_win_ai_4 { get; set; }
        [Column("multi_nato")]
        public int multi_nato {get; set;}
        [Column("multi_played")]
        public int multi_played {get; set;}
        [Column("multi_loss")]
        public int multi_loss {get; set;}
        [Column("multi_last_game")]
        public int multi_last_game { get; set; }
        [Column("xp_multi")]
        public int xp_multi { get; set; }
        [Column("multi_win")]
        public int multi_win { get; set; }
        [Column("time_multi_played")]
        public int time_multi_played { get; set; }
        [Column("multi_pact")]
        public int multi_pact { get; set; }
        [Column("skirmish_last_game")]
        public int skirmish_last_game { get; set; }
        [Column("campaign_pact")]
        public int campaign_pact { get; set; }
        [Column("campaign_last_game")]
        public int campaign_last_game { get; set; }
        [Column("time_campaign_played")]
        public int time_campaign_played { get; set; }
        [Column("time_skirmish_played")]
        public int time_skirmish_played { get; set; }
        [Column("time_tutorial_played")]
        public int time_tutorial_played { get; set; }
        [Column("total_unit_bought")]
        public int total_unit_bought { get; set; }
        [Column("nb_sup_bought")]
        public int nb_sup_bought { get; set; }
        [Column("nb_inf_bought")]
        public int nb_inf_bought { get; set; }
        [Column("nb_art_bought")]
        public int nb_art_bought { get; set; }
        [Column("nb_tank_bought")]
        public int nb_tank_bought { get; set; }
        [Column("nb_reco_bought")]
        public int nb_reco_bought { get; set; }
        [Column("nb_at_bought")]
        public int nb_at_bought { get; set; }
    }
}