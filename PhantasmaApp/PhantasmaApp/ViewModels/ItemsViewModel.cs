using System;
using System.Diagnostics;
using System.Threading.Tasks;

using PhantasmaApp.Helpers;
using PhantasmaApp.Models;
using PhantasmaApp.Views;
using PhantasmaApp.Services;

using Xamarin.Forms;

namespace PhantasmaApp.ViewModels
{
	public class ItemsViewModel : BaseViewModel
	{
        /// <summary>
        /// Get the azure service instance
        /// </summary>
        public DataStore Store { get; private set; }

        public ObservableRangeCollection<Item> Items { get; set; }
		public Command LoadItemsCommand { get; set; }

		public ItemsViewModel(string filter)
        {
            Store = new DataStore(filter);

            Title = filter;
			Items = new ObservableRangeCollection<Item>();
			LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

			MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
			{
				var _item = item as Item;
				Items.Add(_item);
				await Store.AddItemAsync(_item);
			});
		}

		async Task ExecuteLoadItemsCommand()
		{
			if (IsBusy)
				return;

			IsBusy = true;

			try
			{
				Items.Clear();
				var items = await Store.GetItemsAsync(true);
				Items.ReplaceRange(items);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				MessagingCenter.Send(new MessagingCenterAlert
				{
					Title = "Error",
					Message = "Unable to load items.",
					Cancel = "OK"
				}, "message");
			}
			finally
			{
				IsBusy = false;
			}
		}
	}
}