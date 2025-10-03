namespace GameApi.Models.DND2014
{
    public class Condition
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Desc { get; set; } = new List<string>();
        public string Url { get; set; } = string.Empty;
    }
}
