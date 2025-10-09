using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AWSServerSelector
{
    public class LocalizedString : INotifyPropertyChanged
    {
        private string _key;
        private object[] _args;

        public LocalizedString(string key, params object[] args)
        {
            _key = key;
            _args = args;
        }

        public string Value => _args?.Length > 0 
            ? LocalizationManager.GetString(_key, _args) 
            : LocalizationManager.GetString(_key);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static implicit operator string(LocalizedString localizedString)
        {
            return localizedString.Value;
        }
    }
}
