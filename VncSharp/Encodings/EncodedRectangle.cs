// VncSharp - .NET VNC Client Library
// Copyright (C) 2008 David Humphrey
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace VncSharp.Encodings
{
    /// <summary>
    ///     Abstract class representing an Encoded Rectangle to be read, decoded, and drawn.
    /// </summary>
    public abstract class EncodedRectangle : IDesktopUpdater
    {
        protected Framebuffer framebuffer;
        protected PixelReader preader;
        protected Rectangle rectangle;
        protected RfbProtocol rfb;

        protected EncodedRectangle(RfbProtocol rfb, Framebuffer framebuffer, Rectangle rectangle, int encoding)
        {
            this.rfb = rfb;
            this.framebuffer = framebuffer;
            this.rectangle = rectangle;

            //Select appropriate reader
            var reader = encoding == RfbProtocol.ZRLE_ENCODING ? rfb.ZrleReader : rfb.Reader;

            // Create the appropriate PixelReader depending on screen size and encoding
            switch (framebuffer.BitsPerPixel)
            {
                case 32:
                    switch (encoding)
                    {
                        case RfbProtocol.TIGHT_ENCODING:
                            preader = new TPixelReader(reader, framebuffer);
                            break;
                        case RfbProtocol.ZRLE_ENCODING:
                            preader = new CPixelReader(reader, framebuffer);
                            break;
                        default:
                            preader = new PixelReader32(reader, framebuffer);
                            break;
                    }

                    break;
                case 16:
                    preader = new PixelReader16(reader, framebuffer);
                    break;
                case 8:
                    preader = new PixelReader8(reader, framebuffer, rfb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("BitsPerPixel", framebuffer.BitsPerPixel,
                        "Valid VNC Pixel Widths are 8, 16 or 32 bits.");
            }
        }

        /// <summary>
        ///     Gets the rectangle that needs to be decoded and drawn.
        /// </summary>
        public Rectangle UpdateRectangle => rectangle;

        /// <summary>
        ///     Obtain all necessary information from VNC Host (i.e., read) in order to Draw the rectangle, and store in colours[].
        /// </summary>
        public abstract void Decode();

        /// <summary>
        ///     Fills the given Rectangle with a solid colour (i.e., all pixels will have the same value--colour).
        /// </summary>
        /// <param name="rect">The rectangle to be filled.</param>
        /// <param name="colour">The colour to use when filling the rectangle.</param>
        public void FillRectangle(Rectangle rect, int colour)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; ++y)
            {
                for (var x = rect.X; x < rect.X + rect.Width; ++x)
                    framebuffer[y * framebuffer.Width + x] = colour; // colour every pixel the same
            }
        }

        public void FillRectangle(Rectangle rect, int[] tile)
        {
            var idx = 0;
            for (var y = rect.Y; y < rect.Y + rect.Height; ++y)
            {
                for (var x = rect.X; x < rect.X + rect.Width; ++x) 
                    framebuffer[y * framebuffer.Width + x] = tile[idx++];
            }
        }

        /// <summary>
        ///     Fills the given Rectangle with pixel values read from the server (i.e., each pixel may have its own value).
        /// </summary>
        /// <param name="rect">The rectangle to be filled.</param>
        public void FillRectangle(Rectangle rect)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; ++y)
            {
                for (var x = rect.X; x < rect.X + rect.Width; ++x)
                    framebuffer[y * framebuffer.Width + x] = preader.ReadPixel(); // every pixel needs to be read from server
            }
        }
    }
}