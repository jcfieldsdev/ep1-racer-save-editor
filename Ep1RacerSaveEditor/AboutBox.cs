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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ep1RacerSaveEditor
{
    partial class AboutBox : Form
    {
        private const string AppTitle = "Star Wars Episode I: Racer Save Editor";
        private const string Copyright = "Written by J.C. Fields";
        private const string WebSiteUrl = "https://github.com/jcfieldsdev/ep1-racer-save-editor";

        public AboutBox()
        {
            InitializeComponent();

            Text = $"About {AppTitle}";
            labelProductName.Text = AppTitle;
            labelCopyright.Text = Copyright;
            labelWebSiteUrl.Text = WebSiteUrl;
        }

        public void OpenWebSite()
        {
            System.Diagnostics.Process.Start(WebSiteUrl);
        }

        private void ClickVisitWebSiteButton(object sender, EventArgs e)
        {
            OpenWebSite();
        }
    }
}
