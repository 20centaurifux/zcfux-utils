﻿/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Security.Cryptography;
using CommandLine;
using zcfux.Byte;

namespace zcfux.Security;

sealed class Pbkdf2 : IPasswordHashAlgorithm
{
    sealed class Options
    {
        [Option("iterations", Required = true)]
        public int Iterations { get; set; }

        [Option("hash-size", Required = true)]
        public int HashSize { get; set; }
    }

    readonly string[] _args;
    Options _opts = null!;

    public Pbkdf2(string[] args)
    {
        _args = args;

        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(opts =>
            {
                _opts = opts;
            })
            .WithNotParsed(errors =>
            {
                if (errors.Any(err => err.Tag is ErrorType.MissingRequiredOptionError or ErrorType.MissingValueOptionError))
                {
                    throw new ArgumentException("Missing argument.");
                }
            });
    }

    public PasswordHash ComputeHash(string plain, byte[] salt)
    {
        var pbkdf2 = new Rfc2898DeriveBytes(plain.GetBytes(), salt, _opts.Iterations);

        var digest = pbkdf2.GetBytes(_opts.HashSize);

        return new PasswordHash("PBKDF2", digest, salt, _args);
    }
}