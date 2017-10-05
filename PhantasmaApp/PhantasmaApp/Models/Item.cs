namespace PhantasmaApp.Models
{
    public class Item : BaseDataObject
	{
		string author = string.Empty;
		public string Author
		{
			get { return author; }
			set { SetProperty(ref author, value); }
		}

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        string time = string.Empty;
        public string Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }

        string content = string.Empty;
        public string Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

    }
}
