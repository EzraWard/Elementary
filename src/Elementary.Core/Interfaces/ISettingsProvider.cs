using System;
using System.Collections.Generic;
using System.Text;

namespace Elementary.Core.Interfaces
{
    public interface ISettingsProvider
    {
        string GetSetting(string key);

        void SaveSetting(string key, string value);

        void DeleteSetting(string key);
    }
}
