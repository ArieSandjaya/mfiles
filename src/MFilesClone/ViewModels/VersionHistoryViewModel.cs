using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MFilesClone.Shared;

namespace MFilesClone.ViewModels;

public partial class VersionHistoryViewModel : ViewModelBase
{
    public ObservableCollection<DocumentVersionDto> Versions { get; } = new();

    [ObservableProperty]
    private DocumentVersionDto? selectedVersion;

    public void SetVersions(IEnumerable<DocumentVersionDto> versions)
    {
        Versions.ReplaceAll(versions);
    }
}
