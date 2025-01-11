using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        public Bible GetBible(ETranslation translation)
        {
            switch (translation)
            {
                case ETranslation.ASV:
                    return GetBibleASV();
                case ETranslation.KJV:
                    return GetBibleKJV();
                case ETranslation.NET:
                    return GetBibleNET();
                default:
                    return null;
            }
        }

        private Bible GetBibleASV()
        {
            throw new NotImplementedException();
        }

        private Bible GetBibleKJV()
        {
            throw new NotImplementedException();
        }

        private Bible GetBibleNET()
        {
            throw new NotImplementedException();
        }
    }
}