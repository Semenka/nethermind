//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Security;
using Nethermind.KeyStore.Config;

namespace Nethermind.KeyStore
{
    public class BlockAuthorPasswordProvider : IPasswordProvider
    {
        private readonly IKeyStoreConfig _keyStoreConfig;
        private readonly PasswordProviderHelper _passwordProviderHelper;
        private readonly KeyStorePasswordProvider _keyStorePasswordProvider;

        public BlockAuthorPasswordProvider(IKeyStoreConfig keyStoreConfig)
        {
            _keyStoreConfig = keyStoreConfig ?? throw new ArgumentNullException(nameof(keyStoreConfig));
            _passwordProviderHelper = new PasswordProviderHelper();
            _keyStorePasswordProvider = new KeyStorePasswordProvider(keyStoreConfig);
        }
        public SecureString GetPassword(int? passwordIndex = null)
        {
            SecureString passwordFromFile = null;
            var index = Array.IndexOf(_keyStoreConfig.UnlockAccounts, _keyStoreConfig.BlockAuthorAccount);
            if (index >= 0)
            {
                passwordFromFile = _keyStorePasswordProvider.GetPassword(index);
            }

            return passwordFromFile != null ?
                   passwordFromFile 
                   : _passwordProviderHelper.GetPasswordFromConsole($"Provide password for validator account { _keyStoreConfig.BlockAuthorAccount}");
        }
    }
}
