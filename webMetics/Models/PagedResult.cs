namespace webMetics.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();

        public int TotalRecords { get; set; }

        public int FilteredRecords { get; set; }
    }
}
