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
    class SaveFile
    {
        enum Format
        {
            FileSize = 4056,
            ProfileSize = 84
        }

        enum Count
        {
            Profiles = 4,
            AllTracks = 25,
            Positions = 2
        }

        enum Offset
        {
            Profiles = 0x018,
            LapTimes = 0x220,
            TotalTimes = 0x158,
            LapNames = 0x928,
            TotalNames = 0x2e8,
            LapPodracers = 0xf9a,
            TotalPodracers = 0xf68
        }

        enum Length
        {
            Profile = 80,
            Time = 4,
            Name = 32
        }

        enum Max
        {
            Podracer = 22
        }

        private readonly byte[] Header = new byte[] { 0x03, 0x00, 0x01, 0x00 };
        private readonly byte[] DefaultTime = new byte[] { 0xd7, 0xff, 0x60, 0x45 };

        private readonly SaveSlot[] _slots;

        private string _path;
        private byte[] _data;
        private Time[] _lapTimes;
        private Time[] _totalTimes;
        private string[] _lapNames;
        private string[] _totalNames;
        private int[] _lapPodracers;
        private int[] _totalPodracers;

        public string Path { get => _path; set => _path = value; }
        public Time[] LapTimes { get => _lapTimes; set => _lapTimes = value; }
        public Time[] TotalTimes { get => _totalTimes; set => _totalTimes = value; }
        public string[] LapNames { get => _lapNames; set => _lapNames = value; }
        public string[] TotalNames { get => _totalNames; set => _totalNames = value; }
        public int[] LapPodracers { get => _lapPodracers; set => _lapPodracers = value; }
        public int[] TotalPodracers { get => _totalPodracers; set => _totalPodracers = value; }

        public SaveSlot this[int index]
        {
            get => _slots[index];
            set => _slots[index] = value;
        }

        public SaveFile(string path = "")
        {
            _data = path == "" ? NewFile() : OpenFile(path);
            _path = path;

            _slots = ReadProfiles((int)Offset.Profiles, (int)Count.Profiles, (int)Length.Profile);
            
            _lapTimes = ReadTimes((int)Offset.LapTimes, (int)Length.Time);
            _totalTimes = ReadTimes((int)Offset.TotalTimes, (int)Length.Time);

            _lapNames = ReadNames((int)Offset.LapNames, (int)Length.Name);
            _totalNames = ReadNames((int)Offset.TotalNames, (int)Length.Name);

            _lapPodracers = ReadNumbers((int)Offset.LapPodracers, (int)Max.Podracer);
            _totalPodracers = ReadNumbers((int)Offset.TotalPodracers, (int)Max.Podracer);
        }

        public byte[] NewFile()
        {
            return Properties.Resources.tgfd;
        }

        public byte[] OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("The specified file does not exist.");
            }

            var fileInfo = new FileInfo(path);

            if (fileInfo.Length != (int)Format.FileSize)
            {
                throw new Exception("The specified file is not a valid save file.");
            }

            byte[] data = File.ReadAllBytes(path);
            bool validHeader = data[0] == Header[0]
                && data[1] == Header[1]
                && data[2] == Header[2]
                && data[3] == Header[3];

            if (!validHeader)
            {
                throw new Exception("The specified file has an invalid header.");
            }

            return data;
        }

        public void ReplaceSlot(string path, int n)
        {
            if (!File.Exists(path))
            {
                throw new Exception("The specified file does not exist.");
            }

            var fileInfo = new FileInfo(path);

            if (fileInfo.Length != (int)Format.ProfileSize)
            {
                throw new Exception("The specified file is not a valid profile file.");
            }

            byte[] data = File.ReadAllBytes(path);
            bool validHeader = data[0] == Header[0]
                && data[1] == Header[1]
                && data[2] == Header[2]
                && data[3] == Header[3];

            if (!validHeader)
            {
                throw new Exception("The specified file has an invalid header.");
            }

            var slice = data.Skip(Header.Length);
            _slots[n] = new SaveSlot(slice.ToArray());
        }

        public void ResetSlot(int n)
        {
            var slice = NewFile().Skip((int)Offset.Profiles + n * (int)Length.Profile).Take((int)Length.Profile);
            _slots[n] = new SaveSlot(slice.ToArray());
        }

        public SaveSlot[] ReadProfiles(int offset, int count, int length)
        {
            SaveSlot[] slots = new SaveSlot[count];

            for (int i = 0; i < count; i++)
            {
                var slice = _data.Skip(offset + i * length).Take(length);
                slots[i] = new SaveSlot(slice.ToArray());
            }

            return slots;
        }

        public Time[] ReadTimes(int offset, int length)
        {
            int size = (int)Count.AllTracks * (int)Count.Positions;
            byte[] bytes = _data.Skip(offset).Take(size * length).ToArray();
            Time[] times = new Time[size];

            for (int i = 0; i < size; i++)
            {
                times[i] = new Time(BitConverter.ToSingle(bytes, i * length));
            }

            return times;
        }

        public string[] ReadNames(int offset, int length)
        {
            int size = (int)Count.AllTracks * (int)Count.Positions;
            byte[] bytes = _data.Skip(offset).Take(size * length).ToArray();
            string[] names = new string[size];

            for (int i = 0; i < size; i++)
            {
                string fullString = Encoding.ASCII.GetString(bytes, i * length, length);
                int nullPos = fullString.IndexOf('\0');

                // reads string up to null byte
                names[i] = fullString.Substring(0, nullPos > 0 ? nullPos : 0);
            }

            return names;
        }

        public int[] ReadNumbers(int offset, int max)
        {
            int size = (int)Count.AllTracks * (int)Count.Positions;
            byte[] bytes = _data.Skip(offset).Take(size).ToArray();
            int[] numbers = new int[size];

            for (int i = 0; i < size; i++)
            {
                numbers[i] = Math.Min((int)bytes[i], max);
            }

            return numbers;
        }

        public void WriteFile()
        {
            _data = NewFile();

            SaveProfiles((int)Offset.Profiles, (int)Length.Profile);

            SaveTimes(_lapTimes, (int)Offset.LapTimes, (int)Length.Time);
            SaveTimes(_totalTimes, (int)Offset.TotalTimes, (int)Length.Time);

            SaveNames(_lapNames, (int)Offset.LapNames, (int)Length.Name);
            SaveNames(_totalNames, (int)Offset.TotalNames, (int)Length.Name);

            SaveNumber(_lapPodracers, (int)Offset.LapPodracers);
            SaveNumber(_totalPodracers, (int)Offset.TotalPodracers);

            File.WriteAllBytes(_path, _data);
        }

        public void SaveProfiles(int offset, int length)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                byte[] bytes = _slots[i].SaveChanges();
                int pos = offset + i * length;

                for (int j = 0; j < bytes.Length; j++)
                {
                    _data[pos + j] = bytes[j];
                }
            }
        }

        public void SaveTimes(Time[] times, int offset, int length)
        {
            for (int i = 0; i < times.Length; i++)
            {
                int pos = offset + i * length;
                byte[] bytes;

                if (times[i].Min == 0 && times[i].Sec == 0 && times[i].Msec == 0)
                {
                    bytes = DefaultTime;
                }
                else
                {
                    bytes = BitConverter.GetBytes(times[i].Calculate());
                }

                for (int j = 0; j < length; j++)
                {
                    _data[pos + j] = bytes[j];
                }
            }
        }

        public void SaveNames(string[] names, int offset, int length)
        {
            for (int i = 0; i < names.Length; i++)
            {
                int pos = offset + i * length;
                byte[] bytes;

                if (names[i].Length == 0)
                {
                    bytes = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat('A', length)));
                } else
                {
                    bytes = Encoding.ASCII.GetBytes(names[i].PadRight(length, '\0'));
                }

                for (int j = 0; j < length; j++)
                {
                    _data[pos + j] = bytes[j];
                }
            }
        }

        public void SaveNumber(int[] numbers, int offset)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                _data[offset + i] = (byte)numbers[i];
            }
        }
    }
}
