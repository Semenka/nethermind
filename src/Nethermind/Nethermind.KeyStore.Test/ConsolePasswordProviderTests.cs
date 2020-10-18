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

using NUnit.Framework;

namespace Nethermind.KeyStore.Test
{
    public class ConsolePasswordProviderTests
    {
        [Test]
        public void Alternative_provider_sets_correctly()
        {
            var emptyPasswordProvider = new FilePasswordProvider()
                                        { FileName = string.Empty };
            var consolePasswordProvider1 = emptyPasswordProvider
                                            .OrReadFromConsole("Test1");

            Assert.IsTrue(consolePasswordProvider1 is FilePasswordProvider);
            Assert.AreEqual("Test1",((ConsolePasswordProvider)consolePasswordProvider1.AlternativeProvider).Message);

            var consolePasswordProvider2 = consolePasswordProvider1
                                            .OrReadFromConsole("Test2");

            Assert.IsTrue(consolePasswordProvider2 is FilePasswordProvider);
            Assert.AreEqual("Test2", ((ConsolePasswordProvider)consolePasswordProvider2.AlternativeProvider).Message);
        }
    }
}
