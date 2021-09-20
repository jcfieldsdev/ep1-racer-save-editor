/******************************************************************************
 * Star Wars: Episode I Racer Save Editor                                     *
 *                                                                            *
 * Copyright (C) 2021 J.C. Fields (jcfields@jcfields.dev).                    *
 *                                                                            *
 * This program is free software: you can redistribute it and/or modify it    *
 * under the terms of the GNU General Public License as published by the Free *
 * Software Foundation, either version 3 of the License, or (at your option)  *
 * any later version.                                                         *
 *                                                                            *
 * This program is distributed in the hope that it will be useful, but        *
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY *
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License   *
 * for more details.                                                          *
 *                                                                            *
 * You should have received a copy of the GNU General Public License along    *
 * with this program.  If not, see <http://www.gnu.org/licenses/>.            *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ep1RacerSaveEditor
{
    class SaveSlot
    {
        enum Offset {
            PlayerName = 0x00,
            LastPodracer = 0x24,
            Tracks = 0x25,
            Ranks = 0x2a,
            Podracers = 0x34,
            Truguts = 0x38,
            PitDroids = 0x40,
            PartLevels = 0x41,
            PartHealth = 0x48
        }

        enum Length
        {
            Name = 32,
            LastPodracer = 1,
            Truguts = 4,
            Tracks = 4,
            Ranks = 8,
            PitDroids = 1,
            Podracers = 4,
            Parts = 7
        }

        enum Max
        {
            Podracers = 22,
            Truguts = 99_999,
            PitDroids = 4,
            PartLevels = 5,
            PartHealth = 255
        }

        enum Min
        {
            PitDroids = 1
        }

        private readonly byte[] Header = new byte[] { 0x03, 0x00, 0x01, 0x00 };

        private string _playerName;
        private int _lastPodracer;
        private int _truguts;
        private int _pitDroids;
        private bool[] _tracks;
        private bool[] _podracers;
        public int[] _ranks;
        public int[] _partLevels;
        public int[] _partHealth;
        
        private readonly byte[] _data;

        public string PlayerName
        {
            get => _playerName;
            set
            {
                // truncates string to allowed length (minus one for null terminator)
                _playerName = value.Substring(0, Math.Min(value.Length, (int)Length.Name - 1));
            }
        }

        public int LastPodracer { get => _lastPodracer; set => _lastPodracer = value; }
        public int Truguts { get => _truguts; set => _truguts = value; }
        public int PitDroids { get => _pitDroids; set => _pitDroids = value; }
        public bool[] Tracks { get => _tracks; set => _tracks = value; }
        public bool[] Podracers { get => _podracers; set => _podracers = value; }
        public int[] Ranks { get => _ranks; set => _ranks = value; }
        public int[] PartLevels { get => _partLevels; set => _partLevels = value; }
        public int[] PartHealth { get => _partHealth; set => _partHealth = value; }

        public SaveSlot(byte[] data)
        {
            _data = data;

            _tracks = ReadBits((int)Offset.Tracks, (int)Length.Tracks);
            _podracers = ReadBits((int)Offset.Podracers, (int)Length.Podracers);

            _ranks = ReadRanks((int)Offset.Ranks, (int)Length.Ranks);

            _lastPodracer = ReadInteger((int)Offset.LastPodracer, (int)Length.LastPodracer, (int)Max.Podracers);
            _pitDroids = ReadInteger((int)Offset.PitDroids, (int)Length.PitDroids, (int)Max.PitDroids, (int)Min.PitDroids);
            _truguts = ReadInteger((int)Offset.Truguts, (int)Length.Truguts, (int)Max.Truguts);

            _partLevels = ReadNumbers((int)Offset.PartLevels, (int)Length.Parts, (int)Max.PartLevels);
            _partHealth = ReadNumbers((int)Offset.PartHealth, (int)Length.Parts, (int)Max.PartHealth);

            _playerName = ReadString((int)Offset.PlayerName, (int)Length.Name);
        }

        private bool[] ReadBits(int offset, int length)
        {
            byte[] bytes = _data.Skip(offset).Take(length).ToArray();
            bool[] bits = new bool[length * 8];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bits[i * 8 + j] = Convert.ToBoolean(bytes[i] & 1 << j);
                }
            }

            return bits;
        }

        private int[] ReadRanks(int offset, int length)
        {
            byte[] bytes = _data.Skip(offset).Take(length * 8).ToArray();
            int[] ranks = new int[length * 4];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int rank = bytes[i] & 0b11 << j * 2;
                    ranks[i * 4 + j] = rank >> j * 2;
                }
            }

            return ranks;
        }

        private int ReadInteger(int offset, int length, int max, int min = 0)
        {
            byte[] bytes = _data.Skip(offset).Take(length).ToArray();
            int number = 0;

            for (int i = 0; i < length; i++)
            {
                number += bytes[i] << i * 8;
            }

            return Math.Max(Math.Min(number, max), min);
        }

        private int[] ReadNumbers(int offset, int length, int max)
        {
            byte[] bytes = _data.Skip(offset).Take(length).ToArray();
            int[] numbers = new int[length];

            for (int i = 0; i < length; i++)
            {
                numbers[i] = Math.Min(bytes[i], max);
            }

            return numbers;
        }

        private string ReadString(int offset, int length)
        {
            string fullString = Encoding.ASCII.GetString(_data, offset, length);
            int nullPos = fullString.IndexOf('\0');

            // reads string up to null byte
            return fullString.Substring(0, nullPos > 0 ? nullPos : 0);
        }

        public void WriteFile(string path)
        {
            byte[] data = SaveChanges();
            var contents = new List<byte>(Header.Length + data.Length);
            contents.AddRange(Header);
            contents.AddRange(data);
            
            File.WriteAllBytes(path, contents.ToArray());
        }

        public byte[] SaveChanges()
        {
            SaveBits(_tracks, (int)Offset.Tracks, (int)Length.Tracks);
            SaveBits(_podracers, (int)Offset.Podracers, (int)Length.Podracers);

            SaveRanks(_ranks, (int)Offset.Ranks, (int)Length.Ranks);

            SaveInteger(_lastPodracer, (int)Offset.LastPodracer, (int)Length.LastPodracer);
            SaveInteger(_pitDroids, (int)Offset.PitDroids, (int)Length.PitDroids);
            SaveInteger(_truguts, (int)Offset.Truguts, (int)Length.Truguts);

            SaveNumbers(_partLevels, (int)Offset.PartLevels);
            SaveNumbers(_partHealth, (int)Offset.PartHealth);

            SaveString(_playerName, (int)Offset.PlayerName, (int)Length.Name);

            return _data;
        }

        public void SaveBits(bool[] bits, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                _data[offset + i] = 0;

                for (int j = 0; j < 8; j++)
                {
                    _data[offset + i] |= (byte)(Convert.ToByte(bits[i * 8 + j]) << j);
                }
            }
        }

        public void SaveRanks(int[] ranks, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                _data[offset + i] = 0;

                for (int j = 0; j < 4; j++)
                {
                    _data[offset + i] |= (byte)(Convert.ToByte(ranks[i * 4 + j]) << j * 2);
                }
            }
        }

        public void SaveInteger(int number, int offset, int length)
        {
            byte[] bytes = BitConverter.GetBytes(number);

            for (int i = 0; i < length; i++)
            {
                _data[offset + i] = bytes[i];
            }
        }

        public void SaveNumbers(int[] numbers, int offset)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                _data[offset + i] = (byte)numbers[i];
            }
        }

        public void SaveString(string name, int offset, int length)
        {
            byte[] bytes = new byte[length];
            
            if (name.Length > 0)
            {
                bytes = Encoding.ASCII.GetBytes(name.PadRight(length, '\0'));
            }

            for (int i = 0; i < length; i++)
            {
                _data[offset + i] = bytes[i];
            }
        }
    }
}
