using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCodeLeveler
{
    class Program
    {
        static StreamWriter _sw = null;
        static double _x = 0;
        static double _y = 0;
        static double _z = 0;
        static double _zSafe = 0;
        static double _d = 0;
        static uint _n = 0;
        static double _f = 0;
        static double _s = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">
        /// -x [x width]
        /// -y [y width]
        /// -z [z depth]
        /// -zs [z height for traveling]
        /// -d [tool diameter]
        /// -n [number of passes]
        /// -f [feedrate]
        /// -s [spindle rotation speed]
        /// </param>
        static void Main(string[] args)
        {
            try
            {
                if (args.Count() != 16)
                {
                    throw new ArgumentException("Please fill in the following information: -x [x] -y [y] -z [z] -zs [zSafe] -d [tool diameter] -n [# of passes] -f [feedrate] -s [spindle feedrate]!");
                }
                // Parse argument input
#if DEBUG
                Console.WriteLine("-----INPUT DATA-----");
                for (int i = 0; i < args.Count(); i++)
                {
                    Console.WriteLine("[{0}]:{1}", i, args[i]);
                }
                Console.WriteLine("-----INPUT DATA-----");
#endif
                for (int i = 0; i < args.Count(); i += 2)
                {
                    // X width
                    if (args[i].Equals("-x") || args[i].Equals("-X"))
                    {
                        _x = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // Y width
                    else if (args[i].Equals("-y") || args[i].Equals("-Y"))
                    {
                        _y = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // Z width
                    else if (args[i].Equals("-z") || args[i].Equals("-Z"))
                    {
                        _z = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // Z safe height
                    else if (args[i].Equals("-zs") || args[i].Equals("-ZS"))
                    {
                        _zSafe = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // tool diameter
                    else if (args[i].Equals("-d") || args[i].Equals("-D"))
                    {
                        _d = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // number of passes
                    else if (args[i].Equals("-n") || args[i].Equals("-N"))
                    {
                        _n = uint.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // feedrate
                    else if (args[i].Equals("-f") || args[i].Equals("-F"))
                    {
                        _f = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    // spindle speed
                    else if (args[i].Equals("-s") || args[i].Equals("-S"))
                    {
                        _s = double.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("Unknown argument: {0}"), args[i]);
                    }
                }
                // Output the input data
                Console.WriteLine("X: {0}mm", _x);
                Console.WriteLine("Y: {0}mm", _y);
                Console.WriteLine("Z: {0}mm", _z);
                Console.WriteLine("Tool diameter: {0}mm", _d);
                Console.WriteLine("Number of passes: {0}", _n);
                Console.WriteLine("Feedrate: {0}mm/min", _f);
                Console.WriteLine("Spindle speed: {0}mm/min", _s);
                // Open stream writer
                _sw = new StreamWriter("Leveler.nc", false);
                _sw.WriteLine(String.Format("M3 S{0}", _s)); // Turn spindle on clockwise and add spindle feedrate in RPM
                _sw.WriteLine("G21");   // All units in mm
                _sw.WriteLine(String.Format("G01 Z{0}", _zSafe.ToString(CultureInfo.InvariantCulture)));  // Move spindle to safe travel height                
                // Begin plane algo                
                // Z
                double zDelta = _z / _n;                
                for (double z = zDelta; z >= _z; z += zDelta)
                {
                    bool end = false;
                    _sw.WriteLine("G00 X0 Y0"); // Move to [0|0|{zSafe}]
                    _sw.WriteLine(String.Format("G00 Z{0} F{1}", zDelta.ToString(CultureInfo.InvariantCulture), 100)); // Penetrate first layer
                    // Flag for indicating whether the bit is at y max                    
                    // Y
                    for (double y = 0; y <= _y; y += _d / 2)
                    {
                        // Check for end or beginning of xy line
                        if (end == true)
                        {
                            // End of line, increase y, set x to 0
                            _sw.WriteLine(String.Format("G01 X{0} Y{1} Z{2} F{3}", 0, y.ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture), _f.ToString(CultureInfo.InvariantCulture)));
                            // Check for last x pass
                            if (Math.Abs(_y - y) > _d / 2)
                            {
                                _sw.WriteLine(String.Format("G01 X{0} Y{1} Z{2} F{3}", 0, (y + _d / 2).ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture), _f.ToString(CultureInfo.InvariantCulture)));
                            }
                            end = false;
                        }
                        else
                        {
                            // Beginning of line, increase y, set x to x max
                            _sw.WriteLine(String.Format("G01 X{0} Y{1} Z{2} F{3}", _x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture), _f.ToString(CultureInfo.InvariantCulture)));
                            // Check for last x pass
                            if (Math.Abs(_y - y) > _d / 2)
                            {
                                _sw.WriteLine(String.Format("G01 X{0} Y{1} Z{2} F{3}", _x.ToString(CultureInfo.InvariantCulture), (y + _d / 2).ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture), _f.ToString(CultureInfo.InvariantCulture)));
                            }
                            end = true;
                        }
                    }
                    // Lift spindle to safe height
                    _sw.WriteLine("G01 Z{0}", _zSafe.ToString(CultureInfo.InvariantCulture));
                }
                // End of algo
                _sw.WriteLine(String.Format("G00 Z{0}", _zSafe, CultureInfo.InvariantCulture));   // Move z to safe height
                // Spindle off
                _sw.WriteLine("M5");
                // Move to [0|0|{zSafe}]
                _sw.WriteLine("G00 X0 Y0");
                // End program
                _sw.WriteLine("M2");
                Console.WriteLine("File successfully created!");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("The following error occured: {0}", ex.ToString());
            }
            finally
            {
                if (_sw != null)
                {
                    _sw.Close();
                }
#if DEBUG
                Console.ReadLine();
#endif
            }
        }
    }
}
