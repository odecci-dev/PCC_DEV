namespace API_PCC.ApplicationModels
{
    public class BuffAnimalSearchFilterModel
    {
        public string? searchValue { get; set; }
        public BuffAnimalFilterByModel? filterBy {  get; set; }
        public string? sex { get; set; }
        public string? status { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public SortByModel sortBy { get; set; }
    }
}
