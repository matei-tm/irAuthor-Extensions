﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using NuGet;

namespace ExtensionManager.ViewModels
{
    public class ExtensionRowViewModel : INotifyPropertyChanged
    {
        public Guid ExtensionId { get; set; }

        public IPackage Package { get; set; }

        public IPackageMetadata PackageMetadata => Package as IPackageMetadata;

        public string DisplayedVersion => Package.Version.Version.ToString(3);

        private bool updateAvailable;
        public bool UpdateAvailable
        {
            get { return IsInstalled && InstalledVersion != DisplayedVersion; }
        }

        private bool isInstalled;
        public bool IsInstalled
        {
            get { return isInstalled; }
            set
            {
                if (isInstalled != value)
                {
                    isInstalled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsInstalled"));
                }
            }
        }

        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                var shouldRaise = isEnabled == value;
                isEnabled = value;
                if (PropertyChanged != null && shouldRaise)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsEnabled"));
                }
            }
        }

        public IEnumerable<IPackage> AllVersions { get; set; }

        public string InstalledVersion { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}