using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CardArtist
{
    public class Project : ProjectFolder
    {
        public const string CardsFolderName = "Cards";
        public const string TemplatesFolderName = "Templates";

        public readonly string CardsFolderPath;
        public readonly string TemplatesFolderPath;

        public Project(string directory) : base(directory, null)
        {
            CardsFolderPath = Path.Combine(directory, CardsFolderName);
            TemplatesFolderPath = Path.Combine(directory, TemplatesFolderName);

            Directory.CreateDirectory(CardsFolderPath);
            Directory.CreateDirectory(TemplatesFolderPath);
        }

        public ProjectFolder? Templates => Items!.SingleOrDefault(i => i.Name == TemplatesFolderName) as ProjectFolder;
        public ProjectFolder? Cards => Items!.SingleOrDefault(i => i.Name == CardsFolderName) as ProjectFolder;
    }

    public class ProjectItem : INotifyPropertyChanged
    {
        public ProjectItem(string itemPath, ProjectFolder? parent)
        {
            Parent = parent;
            _fullPath = itemPath;
        }

        public string _fullPath;
        public string FullPath
        {
            get => _fullPath;
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Name => Path.GetFileName(FullPath);

        public ProjectFolder? Parent { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProjectFolder : ProjectItem, IDisposable
    {
        private FileSystemWatcher? Watcher;
        private bool IsExpanded;

        public ProjectFolder(string directory, ProjectFolder? parent, bool showChildren = true) : base(directory, parent)
        {
            IsExpanded = showChildren;
            if (showChildren)
            {
                CalculateChildren();
            }
            else
            {
                RemoveChildren();
            }
        }

        public ObservableCollection<ProjectItem>? _items;

        public ObservableCollection<ProjectItem>? Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    if (_items != null)
                    {
                        foreach (var item in _items.OfType<IDisposable>())
                        {
                            item.Dispose();
                        }
                    }
                    _items = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public void Expand()
        {
            IsExpanded = true;
            foreach (var item in Items!.OfType<ProjectFolder>())
            {
                item.CalculateChildren();
            }
        }

        public void Collapse()
        {
            IsExpanded = false;
            foreach (var item in Items!.OfType<ProjectFolder>())
            {
                item.RemoveChildren();
            }
        }

        private void CalculateChildren()
        {
            try
            {
                if (Watcher == null)
                {
                    Watcher = new FileSystemWatcher(FullPath);

                    Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;

                    Watcher.Created += Watcher_Created;
                    Watcher.Deleted += Watcher_Deleted;
                    Watcher.Renamed += Watcher_Renamed;
                    Watcher.Error += Watcher_Error;
                    Watcher.EnableRaisingEvents = true;
                }
                Items = new ObservableCollection<ProjectItem>(
                    Directory.GetDirectories(FullPath)
                        .Select(d => new ProjectFolder(d, this, false))
                        .OrderBy(d => d.Name)
                        .Concat(Directory.GetFiles(FullPath)
                            .Select(d => new ProjectItem(d, this))
                            .OrderBy(d => d.Name)));
            }
            catch
            {
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            MainWindow.Singleton.Dispatcher.BeginInvoke(new Action(() =>
            {
                CalculateChildren();
            }));
        }

        private void OnChildRemoved(string fullPath)
        {
            if (Items != null)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var currentItem = Items[i];
                    if (currentItem.FullPath == fullPath)
                    {
                        Items.RemoveAt(i);
                        (currentItem as IDisposable)?.Dispose();
                        break;
                    }
                }
            }
        }

        private void OnChildAdded(string fullPath)
        {
            var isFile = File.Exists(fullPath);
            ProjectItem newItem;
            Func<ProjectItem, string, bool> itemsFilter;
            if (isFile)
            {
                newItem = new ProjectItem(fullPath, this);
                itemsFilter = (current, name) => !(current is ProjectFolder) && current.Name.CompareTo(name) > 0;
            }
            else
            {
                newItem = new ProjectFolder(fullPath, this, IsExpanded);
                itemsFilter = (current, name) => !(current is ProjectFolder) || current.Name.CompareTo(name) > 0;
            }
            var itemName = newItem.Name;
            int insertPos;
            for (insertPos = 0; insertPos < Items!.Count; insertPos++)
            {
                var current = Items[insertPos];
                if (itemsFilter(current, itemName))
                {
                    break;
                }
            }
            Items.Insert(insertPos, newItem);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            MainWindow.Singleton.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnChildAdded(e.FullPath);
            }));
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            MainWindow.Singleton.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnChildRemoved(e.FullPath);
            }));
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            MainWindow.Singleton.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnChildRemoved(e.OldFullPath);
                OnChildAdded(e.FullPath);
            }));
        }

        private void RemoveChildren()
        {
            Watcher?.Dispose();
            Watcher = null;
            Items = new ObservableCollection<ProjectItem>();
        }

        #region IDisposable
        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Watcher?.Dispose();
                    Items = null; //Disposes Items
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
