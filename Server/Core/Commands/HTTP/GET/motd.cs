namespace HTTP.Metods.GET
{
    public class Motd
    {
        public static string ProcessMotd()
        {
            return "<html><body><h1>403 Forbidden</h1>\n Request forbidden by administrative rules. \n </body></html> \n";
        }
    }
}