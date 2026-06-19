using System.Collections.ObjectModel;

namespace MFilesClone.ViewModels;

internal static class ObservableCollectionExtensions
{
    public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
