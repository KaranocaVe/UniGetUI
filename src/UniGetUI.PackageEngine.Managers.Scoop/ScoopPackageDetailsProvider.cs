﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal class ScoopPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public ScoopPackageDetailsProvider(Scoop manager) : base(manager) { }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            PackageDetails details = new(package);

            if (package.Source.Url != null)
                try
                {
                    details.ManifestUrl = new Uri(package.Source.Url.ToString() + "/blob/master/bucket/" + package.Id + ".json");
                }
                catch (Exception ex)
                {
                    Logger.Error("Cannot load package manifest URL");
                    Logger.Error(ex);
                }

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " cat " + package.Source.Name + "/" + package.Id,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };

            p.Start();
            string JsonString = await p.StandardOutput.ReadToEndAsync();

            JsonObject RawInfo = JsonObject.Parse(JsonString) as JsonObject;

            try
            {
                if (RawInfo.ContainsKey("description") && (RawInfo["description"] is JsonArray))
                {
                    details.Description = "";
                    foreach (JsonNode note in RawInfo["description"] as JsonArray)
                        details.Description += note.ToString() + "\n";
                    details.Description = details.Description.Replace("\n\n", "\n").Trim();
                }
                else if (RawInfo.ContainsKey("description"))
                    details.Description = RawInfo["description"].ToString();
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load description: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("innosetup"))
                    details.InstallerType = "Inno Setup (" + CoreTools.Translate("extracted") + ")";
                else
                    details.InstallerType = CoreTools.Translate("Scoop package");
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load installer type: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("homepage"))
                {
                    details.HomepageUrl = new Uri(RawInfo["homepage"].ToString());
                    if (details.HomepageUrl.ToString().Contains("https://github.com/"))
                        details.Author = details.HomepageUrl.ToString().Replace("https://github.com/", "").Split("/")[0];
                    else
                        details.Author = details.HomepageUrl.Host.Split(".")[^2];
                }
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load homepage: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("notes") && (RawInfo["notes"] is JsonArray))
                {
                    details.ReleaseNotes = "";
                    foreach (JsonNode note in RawInfo["notes"] as JsonArray)
                        details.ReleaseNotes += note.ToString() + "\n";
                    details.ReleaseNotes = details.ReleaseNotes.Replace("\n\n", "\n").Trim();
                }
                else if (RawInfo.ContainsKey("notes"))
                    details.ReleaseNotes = RawInfo["notes"].ToString();
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load notes: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("license"))
                {
                    if (RawInfo["license"] is not JsonValue)
                    {
                        details.License = RawInfo["license"]["identifier"].ToString();
                        details.LicenseUrl = new Uri(RawInfo["license"]["url"].ToString());
                    }
                    else
                        details.License = RawInfo["license"].ToString();
                }
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load license: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("url") && RawInfo.ContainsKey("hash"))
                {
                    if (RawInfo["url"] is JsonArray)
                        details.InstallerUrl = new Uri(RawInfo["url"][0].ToString());
                    else
                        details.InstallerUrl = new Uri(RawInfo["url"].ToString());

                    if (RawInfo["hash"] is JsonArray)
                        details.InstallerHash = RawInfo["hash"][0].ToString();
                    else
                        details.InstallerHash = RawInfo["hash"].ToString();
                }
                else if (RawInfo.ContainsKey("architecture"))
                {
                    string FirstArch = (RawInfo["architecture"] as JsonObject).ElementAt(0).Key;
                    details.InstallerHash = RawInfo["architecture"][FirstArch]["hash"].ToString();
                    details.InstallerUrl = new Uri(RawInfo["architecture"][FirstArch]["url"].ToString());
                }

                details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load installer URL: " + ex); }

            try
            {
                if (RawInfo.ContainsKey("checkver") && RawInfo["checkver"] is JsonObject && (RawInfo["checkver"] as JsonObject).ContainsKey("url"))
                    details.ReleaseNotesUrl = new Uri(RawInfo["checkver"]["url"].ToString());
            }
            catch (Exception ex) { Logger.Debug("[Scoop] Can't load notes URL: " + ex); }

            return details;

        }


        protected override Task<string> GetPackageIcon_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override Task<string[]> GetPackageScreenshots_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            throw new Exception("Scoop does not support custom package versions");
        }
    }
}
