using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DynamicData;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public class HubSettingsViewModel : ViewModelBase
{
    public string[] DefaultHubs => ConfigConstants.DefaultHubUrls.Select(set => set.Urls[0]).ToArray();
    public ObservableCollection<HubViewModel> HubList { get; set; } = new();

    private readonly DataManager _dataManager = Locator.Current.GetRequiredService<DataManager>();

    public void Save()
    {
        var hubs = new List<Hub>();

        for (var i = 0; i < HubList.Count; i++)
        {
            var uri = new Uri(HubList[i].Address, UriKind.Absolute);

            // Automatically add trailing slashes for the user
            if (!uri.AbsoluteUri.EndsWith("/"))
            {
                uri = new Uri(uri.AbsoluteUri + "/", UriKind.Absolute);
            }

            hubs.Add(new Hub(uri, i));
        }

        _dataManager.SetHubs(hubs);
    }

    public void Populate()
    {
        HubList.AddRange(_dataManager.Hubs.OrderBy(h => h.Priority)
            .Select(h => new HubViewModel(h.Address.AbsoluteUri, this, true)));
    }

    public void Add()
    {
        HubList.Add(new HubViewModel("", this));
    }

    private void Reset()
    {
        HubList.Clear();
    }

    public List<string> GetDupes()
    {
        return DefaultHubs
            .Concat(HubList.Select(h => NormalizeHubUri(h.Address))).GroupBy(h => h)
            .Where(group => group.Count() > 1)
            .Select(x => x.Key)
            .ToList();
    }

    public static bool IsValidHubUri(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && string.IsNullOrEmpty(uri.Fragment)
               && string.IsNullOrEmpty(uri.Query);
    }

    [return: NotNullIfNotNull(nameof(address))]
    public static string? NormalizeHubUri(string? address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            return address;

        if (!uri.AbsoluteUri.EndsWith('/'))
        {
            return uri.AbsoluteUri + '/';
        }

        return uri.AbsoluteUri;
    }
}

public class HubViewModel : ViewModelBase
{
    public string Address { get; set; }
    private readonly HubSettingsViewModel _parentVm;
    private bool IsNotDefault { get; }

    public HubViewModel(string address, HubSettingsViewModel parentVm, bool isNotDefault = true)
    {
        Address = address;
        _parentVm = parentVm;
        IsNotDefault = isNotDefault;
    }

    public void Remove()
    {
        _parentVm.HubList.Remove(this);
    }

    public void Up()
    {
        var i = _parentVm.HubList.IndexOf(this);

        if (i == 0)
            return;

        _parentVm.HubList[i] = _parentVm.HubList[i - 1];
        _parentVm.HubList[i - 1] = this;
    }

    public void Down()
    {
        var i = _parentVm.HubList.IndexOf(this);

        if (i == _parentVm.HubList.Count - 1)
            return;

        _parentVm.HubList[i] = _parentVm.HubList[i + 1];
        _parentVm.HubList[i + 1] = this;
    }
}
