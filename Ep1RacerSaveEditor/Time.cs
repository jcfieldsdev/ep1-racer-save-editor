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

public class Time
{
    private int _min;
    private int _sec;
    private float _msec;

    public int Min { get => _min; set => _min = value; }
    public int Sec { get => _sec; set => _sec = value; }
    public int Msec { get => (int)_msec; set => _msec = value; }

    public Time(float totalTime)
    {
        if (Math.Truncate(totalTime * 1000) == 3_599_989)
        {
            _min = 0;
            _sec = 0;
            _msec = 0;
        } else
        {
            _min = (int)(totalTime / 60);
            _sec = (int)(totalTime % 60);
            _msec = (totalTime % 1) * 1000;
        }
    }

    public float Calculate()
    {
        return (_min + _sec / 60f) * 60 + _msec / 1000;
    }
}
