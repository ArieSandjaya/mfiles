using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MFilesClone.Models;

namespace MFilesClone.ViewModels;

public partial class VersionHistoryViewModel : ViewModelBase
{
    public ObservableCollection<DocumentVersion> Versions { get; } = new();

    [ObservableProperty]
    private DocumentVersion? selectedVersion;

    public void SetVersions(IEnumerable<DocumentVersion> versions)
    {
        Versions.ReplaceAll(versions);
    }
}
