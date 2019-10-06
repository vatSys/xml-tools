using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KMLtoMap
{
    class Conversions
    {
        public const double SMALL = 0.01;

        public static double FeetToNauticalMiles(double feet)
        {
            return feet / 6076.12;
        }
        public static double NauticalMilesToFeet(double nm)
        {
            return nm * 6076.12;
        }
        public static double NauticalMilesToMetres(double nm)
        {
            return nm * 1852;
        }
        public static double MetresToNauticalMiles(double m)
        {
            return m / 1852;
        }
        public static double DegreesToRadians(double degrees)
        {
            double rads = degrees / 180 * Math.PI;
            return rads;
        }
        public static double RadiansToDegrees(double rads)
        {
            double degs = rads * 180 / Math.PI;
            return degs;
        }
       
        public static double CalculateTrack(Coordinate latlon1, Coordinate latlon2)
        {
            return RadiansToDegrees(CalculateTrackRADS(latlon1, latlon2));
        }

        public static double CalculateTrackRADS(Coordinate latlon1, Coordinate latlon2)
        {
            double track;
            double[] ll1 = new double[2], ll2 = new double[2];
            ll1[0] = DegreesToRadians(latlon1.Latitude);
            ll1[1] = DegreesToRadians(latlon1.Longitude) * -1;
            ll2[0] = DegreesToRadians(latlon2.Latitude);
            ll2[1] = DegreesToRadians(latlon2.Longitude) * -1;
            track = (Math.Atan2(Math.Sin(ll1[1] - ll2[1]) * Math.Cos(ll2[0]), Math.Cos(ll1[0]) * Math.Sin(ll2[0]) - Math.Sin(ll1[0]) * Math.Cos(ll2[0]) * Math.Cos(ll1[1] - ll2[1]))) % (2 * Math.PI);
            if (track < 0)
                track += 2 * Math.PI;

            return track;
        }

        
        public static double CalculateDistance(Coordinate latlon1, Coordinate latlon2)//returns in NM
        {
            double[] ll1 = new double[2], ll2 = new double[2];
            ll1[0] = DegreesToRadians(latlon1.Latitude);
            ll1[1] = DegreesToRadians(latlon1.Longitude);
            ll2[0] = DegreesToRadians(latlon2.Latitude);
            ll2[1] = DegreesToRadians(latlon2.Longitude);
            double pow1 = Math.Pow(Math.Sin((ll1[0] - ll2[0]) / 2), 2);
            double pow2 = Math.Pow(Math.Sin((ll1[1] - ll2[1]) / 2), 2);
            double dist = 2 * Math.Asin(Math.Sqrt(pow1 + Math.Cos(ll1[0]) * Math.Cos(ll2[0]) * pow2));
            return dist * (180 * 60) / Math.PI;
        }

        public static double CalculateDistanceRADS(Coordinate latlon1, Coordinate latlon2)//returns in NM
        {
            double[] ll1 = new double[2], ll2 = new double[2];
            ll1[0] = DegreesToRadians(latlon1.Latitude);
            ll1[1] = DegreesToRadians(latlon1.Longitude);
            ll2[0] = DegreesToRadians(latlon2.Latitude);
            ll2[1] = DegreesToRadians(latlon2.Longitude);
            double pow1 = Math.Pow(Math.Sin((ll1[0] - ll2[0]) / 2), 2);
            double pow2 = Math.Pow(Math.Sin((ll1[1] - ll2[1]) / 2), 2);
            double dist = 2 * Math.Asin(Math.Sqrt(pow1 + Math.Cos(ll1[0]) * Math.Cos(ll2[0]) * pow2));
            return dist;
        }

        public static Coordinate CalculateLLFromFractionOfRoute(Coordinate routelatlong1, Coordinate routelatlong2, double fraction)
        {
            if (fraction == 0)
                return routelatlong1;
            else if (fraction == 1)
                return routelatlong2;
            else if (routelatlong1 == routelatlong2)
                return routelatlong2;
            else if (fraction > 1 || fraction < 0)
                return null;

        //            A=sin((1-f)*d)/sin(d)
        //B=sin(f*d)/sin(d)
        //x = A*cos(lat1)*cos(lon1) +  B*cos(lat2)*cos(lon2)
        //y = A*cos(lat1)*sin(lon1) +  B*cos(lat2)*sin(lon2)
        //z = A*sin(lat1)           +  B*sin(lat2)
        //lat=atan2(z,sqrt(x^2+y^2))
        //lon=atan2(y,x)
           double lat1,lat2;
            double lon1,lon2;
            lat1 = DegreesToRadians(routelatlong1.Latitude);
            lat2 = DegreesToRadians(routelatlong2.Latitude);
            lon1 = DegreesToRadians(routelatlong1.Longitude);
            lon2 = DegreesToRadians(routelatlong2.Longitude);

            double drads = CalculateDistanceRADS(routelatlong1,routelatlong2);
            double A = Math.Sin((1 - fraction) * drads) / Math.Sin(drads);
            double B = Math.Sin(fraction * drads) / Math.Sin(drads);
            double x = A * Math.Cos(lat1) * Math.Cos(lon1) + B * Math.Cos(lat2) * Math.Cos(lon2);
            double y = A * Math.Cos(lat1) * Math.Sin(lon1) + B * Math.Cos(lat2) * Math.Sin(lon2);
            double z = A * Math.Sin(lat1) + B * Math.Sin(lat2);
            double lat = Math.Atan2(z, Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)));
            double lon = Math.Atan2(y, x);
            return new Coordinate(RadiansToDegrees(lat), RadiansToDegrees(lon));
        }

        public static double CalculateCrossTrackDistance(Coordinate routelatlong1, Coordinate routelatlong2, Coordinate latlong)
        {
            //XTD =asin(sin(dist_AD)*sin(crs_AD-crs_AB))
            double dist = Math.Asin(Math.Sin(CalculateDistanceRADS(routelatlong1,latlong))*Math.Sin(CalculateTrackRADS(routelatlong1,latlong)-CalculateTrackRADS(routelatlong1,routelatlong2)));
            return dist * (180 * 60) / Math.PI;
        }

        public static bool IsLatLonOnGC(Coordinate llgc1, Coordinate llgc2, Coordinate ll)
        {
            //            Latitude of point on GC

            //Intermediate points {lat,lon} lie on the great circle connecting points 1 and 2 when:

            //lat=atan((sin(lat1)*cos(lat2)*sin(lon-lon2)-sin(lat2)*cos(lat1)*sin(lon-lon1))
            //              /(cos(lat1)*cos(lat2)*sin(lon1-lon2)))
            //(not applicable for meridians. i.e if sin(lon1-lon2)=0)

            if (Conversions.CalculateDistance(llgc1, llgc2) - CalculateDistance(ll, llgc2) < 1e-6 || Conversions.CalculateDistance(llgc1, llgc2) - CalculateDistance(ll, llgc1) < 1e-6)
                return false;

            double lat1,lat2;
            double lon1,lon2;
            lat1 = DegreesToRadians(llgc1.Latitude);
            lat2 = DegreesToRadians(llgc2.Latitude);
            lon1 = DegreesToRadians(llgc1.Longitude)*-1;
            lon2 = DegreesToRadians(llgc2.Longitude)*-1;

            double lon = DegreesToRadians(ll.Longitude)*-1;
            double lat = DegreesToRadians(ll.Latitude);

            double testlat = Math.Atan((Math.Sin(lat1) * Math.Cos(lat2) * Math.Sin(lon - lon2) - Math.Sin(lat2) * Math.Cos(lat1) * Math.Sin(lon - lon1)) / (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(lon1 - lon2)));

            if (Math.Abs(testlat-lat)<1e-6)
                return true;
            else
                return false;
        }

        public static double CalculateTrackAtGCLattitude(double gcLat, double gcTrack, double testLat)
        {
            //sin(tc1)*cos(lat1)=sin(tc2)*cos(lat2)
            double gclatrads = DegreesToRadians(gcLat);
            double gctrackrads = DegreesToRadians(gcTrack);
            double latrads = DegreesToRadians(testLat);

            double sintc2 = (Math.Sin(gctrackrads) * Math.Cos(gclatrads)) / Math.Cos(latrads);
            return RadiansToDegrees(Math.Asin(sintc2));
        }

        //public static bool IsLatlonInPoly(Coordinate latlon, List<Coordinate> Poly)
        //{
        //    int nvert, i, j;
        //    bool c = false;

        //    nvert = Poly.Count;
        //    float[] vertx = new float[nvert]; 
        //    float[] verty = new float[nvert];
        //    for(int ii = 0; ii<nvert; ii++)
        //    {
        //        vertx[ii] = Poly[ii].Longitude;
        //        verty[ii] = Poly[ii].Latitude;
        //    }
        //    float testx, testy;
        //    testx = latlon.Longitude;
        //    testy = latlon.Latitude;

        //    for (i = 0, j = nvert - 1; i < nvert; j = i++)
        //    {
        //        if (((verty[i] > testy) != (verty[j] > testy)) &&
        //         (testx < (vertx[j] - vertx[i]) * (testy - verty[i]) / (verty[j] - verty[i]) + vertx[i]))
        //            c = !c;
        //    }
        //    return c;
        //}

        public static List<Coordinate> CreateGreatCircleSegments(Coordinate point1, Coordinate point2, int scale = 100)
        {
            List<Coordinate> points = new List<Coordinate>();
            double dist = CalculateDistance(point1, point2);
            if (dist > scale * 1.5)
            {
                double segsreq = dist / scale;
                for (int i = 1; i < Math.Round(segsreq); i++)
                    points.Add(CalculateLLFromFractionOfRoute(point1, point2, i * scale / dist));
            }
            return points;
        }

        public static bool IsLatlonInPoly(Coordinate latlon, List<Coordinate> Poly)
        {
            var lastPoint = Poly[Poly.Count - 1];
            var isInside = false;
            var x = latlon.Longitude;
            foreach (var point in Poly)
            {
                var x1 = lastPoint.Longitude;
                var x2 = point.Longitude;
                var dx = x2 - x1;

                if (Math.Abs(dx) > 180.0)
                {
                    // we have, most likely, just jumped the dateline (could do further validation to this effect if needed).  normalise the numbers.
                    if (x > 0)
                    {
                        while (x1 < 0)
                            x1 += 360;
                        while (x2 < 0)
                            x2 += 360;
                    }
                    else
                    {
                        while (x1 > 0)
                            x1 -= 360;
                        while (x2 > 0)
                            x2 -= 360;
                    }
                    dx = x2 - x1;
                }

                if ((x1 <= x && x2 > x) || (x1 >= x && x2 < x))
                {
                    var grad = (point.Latitude - lastPoint.Latitude) / dx;
                    var intersectAtLat = lastPoint.Latitude + ((x - x1) * grad);

                    if (intersectAtLat > latlon.Latitude)
                        isInside = !isInside;
                }
                lastPoint = point;
            }

            return isInside;
        }

        //public static bool IsLatlonInPoly2(Coordinate latlon, List<Coordinate> Poly)
        //{
        //    int i, j = Poly.Count - 1;
        //    bool oddNodes = false;

        //    for (i = 0; i < Poly.Count; i++)
        //    {
        //        if ((Poly[i].Latitude < latlon.Latitude && Poly[j].Latitude >= latlon.Latitude
        //        || Poly[j].Latitude < latlon.Latitude && Poly[i].Latitude >= latlon.Latitude)
        //        && (Poly[i].Longitude <= latlon.Longitude || Poly[j].Longitude <= latlon.Longitude))
        //        {
        //            oddNodes ^= (Poly[i].Longitude + (latlon.Latitude - Poly[i].Latitude) / (Poly[j].Latitude - Poly[i].Latitude) * (Poly[j].Longitude - Poly[i].Longitude) < latlon.Longitude);
        //        }
        //        j = i;
        //    }
        //    return oddNodes;
        //}

        //public static List<double[]> CalculateGCOverlapPoly(List<double[]> p1, List<double[]> p2)
        //{
        //    List<double[]> overlap = new List<double[]>();

        //    for (int i = 0; i < p2.Count; i++)
        //    {
        //        if (IsLatlonInPoly(p2[i], p1))
        //            overlap.Add(p2[i]);
        //    }

        //    for (int i = 0; i < p1.Count; i++)
        //    {
        //        if (IsLatlonInPoly(p1[i], p2))
        //            overlap.Add(p1[i]);
        //    }

        //    for (int i = 1; i < p1.Count; i++)
        //    {
        //        double[] ll1 = p1[i - 1];
        //        double[] ll2 = p1[i];

        //        for (int j = 1; j < p2.Count; j++)
        //        {
        //            double[] ll3 = p2[j - 1];
        //            double[] ll4 = p2[j];

        //            double[] x = CalculateGCIntersectionLL(ll1, ll2, ll3, ll4);
        //            if (x != null)
        //                overlap.Add(x);
        //        }
        //    }

        //    if (overlap.Count > 0)
        //        return overlap;
        //    else
        //        return null;
        //}

        public static Coordinate CalculateGCIntersectionLL(Coordinate ll1, Coordinate ll2, Coordinate ll3, Coordinate ll4)
        {
            /* (1) e ={ex,ey,ez} = {cos(lat)*cos(lon), -cos(lat)*sin(lon), sin(lat)}
             * (2) lat=atan2(ez, sqrt(ex^2 + ey^2)); lon=atan2(-ey, ex) 
             * (3)    {e1y *e2z -e2y *e1z, e1z *e2x -e2z *e1x, e1x *e2y -e1y *e2x} 
             * (4)      ||e|| =sqrt(ex^2 + ey^2 + ez^2) 
             * (5)     e1 X e2 = { sin(lat1-lat2) *sin((lon1+lon2)/2) *cos((lon1-lon2)/2) - sin(lat1+lat2) *cos((lon1+lon2)/2) *sin((lon1-lon2)/2) ,
                                    sin(lat1-lat2) *cos((lon1+lon2)/2) *cos((lon1-lon2)/2) + sin(lat1+lat2) *sin((lon1+lon2)/2) *sin((lon1-lon2)/2) ,
                                    cos(lat1)*cos(lat2)*sin(lon1-lon2) } 

                    Algorithm:

                    compute e1 X e2 and e3 X e4 using (5).

                    Normalize ea= (e1 X e2)/ ||e1 X e2|| , eb=(e3 x e4)/||e3 X e4|| using (4)

                    Compute ea X eb using (3)

                    Invert using (2) (it's unnecessary to normalize first).

                    The two candidate intersections are (lat,lon) and the antipodal point

                    (-lat, lon+pi)
             */

            //before we attempt to find intersection, check if it is possible.

            if (CalculateDistance(ll1, ll2) < 1 || CalculateDistance(ll3, ll4) < 1)
                return null;

            //double a1maxlon = Math.Abs(ll2[1]) > Math.Abs(ll1[1]) ? Math.Abs(ll2[1]) : Math.Abs(ll1[1]);
            //double a1minlon = Math.Abs(ll2[1]) > Math.Abs(ll1[1]) ? Math.Abs(ll1[1]) : Math.Abs(ll2[1]);
            //double a2maxlon = Math.Abs(ll4[1]) > Math.Abs(ll3[1]) ? Math.Abs(ll4[1]) : Math.Abs(ll3[1]);
            //double a2minlon = Math.Abs(ll4[1]) > Math.Abs(ll3[1]) ? Math.Abs(ll3[1]) : Math.Abs(ll4[1]);

            //double a1maxlat = Math.Abs(ll2[0]) > Math.Abs(ll1[0]) ? Math.Abs(ll2[0]) : Math.Abs(ll1[0]);
            //double a1minlat = Math.Abs(ll2[0]) > Math.Abs(ll1[0]) ? Math.Abs(ll1[0]) : Math.Abs(ll2[0]);
            //double a2maxlat = Math.Abs(ll4[0]) > Math.Abs(ll3[0]) ? Math.Abs(ll4[0]) : Math.Abs(ll3[0]);
            //double a2minlat = Math.Abs(ll4[0]) > Math.Abs(ll3[0]) ? Math.Abs(ll3[0]) : Math.Abs(ll4[0]);

            //if (a1minlon > a2maxlon || a2minlon > a1maxlon)
            //    return null;

            //convert to radians...
            double lat1 = Conversions.DegreesToRadians(ll1.Latitude);
            double lat2 = Conversions.DegreesToRadians(ll2.Latitude);
            double lat3 = Conversions.DegreesToRadians(ll3.Latitude);
            double lat4 = Conversions.DegreesToRadians(ll4.Latitude);
            double lon1 = Conversions.DegreesToRadians(ll1.Longitude) * -1;//formula uses negative for EAST. convert....
            double lon2 = Conversions.DegreesToRadians(ll2.Longitude) * -1;
            double lon3 = Conversions.DegreesToRadians(ll3.Longitude) * -1;
            double lon4 = Conversions.DegreesToRadians(ll4.Longitude) * -1;

            double[] e1Xe2 = new double[3]{ Math.Sin(lat1-lat2) *Math.Sin((lon1+lon2)/2) *Math.Cos((lon1-lon2)/2) - Math.Sin(lat1+lat2) *Math.Cos((lon1+lon2)/2) *Math.Sin((lon1-lon2)/2) ,
                                            Math.Sin(lat1-lat2) *Math.Cos((lon1+lon2)/2) *Math.Cos((lon1-lon2)/2) + Math.Sin(lat1+lat2) *Math.Sin((lon1+lon2)/2) *Math.Sin((lon1-lon2)/2) ,
                                            Math.Cos(lat1)*Math.Cos(lat2)*Math.Sin(lon1-lon2) };

            double[] e3Xe4 = new double[3]{ Math.Sin(lat3-lat4) *Math.Sin((lon3+lon4)/2) *Math.Cos((lon3-lon4)/2) - Math.Sin(lat3+lat4) *Math.Cos((lon3+lon4)/2) *Math.Sin((lon3-lon4)/2) ,
                                            Math.Sin(lat3-lat4) *Math.Cos((lon3+lon4)/2) *Math.Cos((lon3-lon4)/2) + Math.Sin(lat3+lat4) *Math.Sin((lon3+lon4)/2) *Math.Sin((lon3-lon4)/2) ,
                                            Math.Cos(lat3)*Math.Cos(lat4)*Math.Sin(lon3-lon4) };
            double abse1Xe2 = Math.Sqrt(Math.Pow(e1Xe2[0], 2) + Math.Pow(e1Xe2[1], 2) + Math.Pow(e1Xe2[2], 2));
            double[] ea = new double[3]{e1Xe2[0] / abse1Xe2, 
                                        e1Xe2[1] / abse1Xe2,
                                        e1Xe2[2] / abse1Xe2};
            double abse3Xe4 = Math.Sqrt(Math.Pow(e3Xe4[0], 2) + Math.Pow(e3Xe4[1], 2) + Math.Pow(e3Xe4[2], 2));
            double[] eb = new double[3]{e3Xe4[0] / abse3Xe4, 
                                        e3Xe4[1] / abse3Xe4,
                                        e3Xe4[2] / abse3Xe4};
            double[] eaXeb = new double[3]{ea[1] *eb[2] -eb[1] *ea[2], 
                                           ea[2] *eb[0] -eb[2] *ea[0], 
                                           ea[0] *eb[1] -ea[1] *eb[0]}; 

            double lat = Math.Atan2(eaXeb[2], Math.Sqrt(Math.Pow(eaXeb[0],2) + Math.Pow(eaXeb[1],2))); 
            double lon = Math.Atan2(-eaXeb[1], eaXeb[0]);

            double antilat = lat * -1;
            double antilon = lon - Math.PI;

            //convert back to degrees and invert lon for convention sake
            lat = Conversions.RadiansToDegrees(lat);
            lon = Conversions.RadiansToDegrees(lon)*-1;
            antilat = Conversions.RadiansToDegrees(antilat);
            antilon = Conversions.RadiansToDegrees(antilon) * -1;

            Coordinate ll = new Coordinate(lat, lon);
            //test lengths
            double l1 = CalculateDistance(ll1, ll2);
            double l1ia = CalculateDistance(ll1, ll);
            double l1ib = CalculateDistance(ll2, ll);

            double l2 = CalculateDistance(ll3, ll4);
            double l2ia = CalculateDistance(ll3, ll);
            double l2ib = CalculateDistance(ll4, ll);

            double test1 = l1 - l1ia - l1ib;
            double test2 = l2 - l2ia - l2ib;
            double small = 1e-4;

            if (Math.Abs(test1) < small && Math.Abs(test2) < small)
                return ll;

            ll = new Coordinate(antilat, antilon);
            //test lengths
            l1 = CalculateDistance(ll1, ll2);
            l1ia = CalculateDistance(ll1, ll);
            l1ib = CalculateDistance(ll2, ll);

            l2 = CalculateDistance(ll3, ll4);
            l2ia = CalculateDistance(ll3, ll);
            l2ib = CalculateDistance(ll4, ll);

            test1 = l1 - l1ia - l1ib;
            test2 = l2 - l2ia - l2ib;
            small = 1e-4;

            if (Math.Abs(test1) < small && Math.Abs(test2) < small)
                return ll;


            return null;
        }

        public static List<Coordinate> CalculateAllGCIntersectionLL(Coordinate ll1, Coordinate ll2, Coordinate ll3, Coordinate ll4)
        {
            /* (1) e ={ex,ey,ez} = {cos(lat)*cos(lon), -cos(lat)*sin(lon), sin(lat)}
             * (2) lat=atan2(ez, sqrt(ex^2 + ey^2)); lon=atan2(-ey, ex) 
             * (3)    {e1y *e2z -e2y *e1z, e1z *e2x -e2z *e1x, e1x *e2y -e1y *e2x} 
             * (4)      ||e|| =sqrt(ex^2 + ey^2 + ez^2) 
             * (5)     e1 X e2 = { sin(lat1-lat2) *sin((lon1+lon2)/2) *cos((lon1-lon2)/2) - sin(lat1+lat2) *cos((lon1+lon2)/2) *sin((lon1-lon2)/2) ,
                                    sin(lat1-lat2) *cos((lon1+lon2)/2) *cos((lon1-lon2)/2) + sin(lat1+lat2) *sin((lon1+lon2)/2) *sin((lon1-lon2)/2) ,
                                    cos(lat1)*cos(lat2)*sin(lon1-lon2) } 

                    Algorithm:

                    compute e1 X e2 and e3 X e4 using (5).

                    Normalize ea= (e1 X e2)/ ||e1 X e2|| , eb=(e3 x e4)/||e3 X e4|| using (4)

                    Compute ea X eb using (3)

                    Invert using (2) (it's unnecessary to normalize first).

                    The two candidate intersections are (lat,lon) and the antipodal point

                    (-lat, lon+pi)
             */

            //convert to radians...
            double lat1 = Conversions.DegreesToRadians(ll1.Latitude);
            double lat2 = Conversions.DegreesToRadians(ll2.Latitude);
            double lat3 = Conversions.DegreesToRadians(ll3.Latitude);
            double lat4 = Conversions.DegreesToRadians(ll4.Latitude);
            double lon1 = Conversions.DegreesToRadians(ll1.Longitude) * -1;//formula uses negative for EAST. convert....
            double lon2 = Conversions.DegreesToRadians(ll2.Longitude) * -1;
            double lon3 = Conversions.DegreesToRadians(ll3.Longitude) * -1;
            double lon4 = Conversions.DegreesToRadians(ll4.Longitude) * -1;

            double[] e1Xe2 = new double[3]{ Math.Sin(lat1-lat2) *Math.Sin((lon1+lon2)/2) *Math.Cos((lon1-lon2)/2) - Math.Sin(lat1+lat2) *Math.Cos((lon1+lon2)/2) *Math.Sin((lon1-lon2)/2) ,
                                            Math.Sin(lat1-lat2) *Math.Cos((lon1+lon2)/2) *Math.Cos((lon1-lon2)/2) + Math.Sin(lat1+lat2) *Math.Sin((lon1+lon2)/2) *Math.Sin((lon1-lon2)/2) ,
                                            Math.Cos(lat1)*Math.Cos(lat2)*Math.Sin(lon1-lon2) };

            double[] e3Xe4 = new double[3]{ Math.Sin(lat3-lat4) *Math.Sin((lon3+lon4)/2) *Math.Cos((lon3-lon4)/2) - Math.Sin(lat3+lat4) *Math.Cos((lon3+lon4)/2) *Math.Sin((lon3-lon4)/2) ,
                                            Math.Sin(lat3-lat4) *Math.Cos((lon3+lon4)/2) *Math.Cos((lon3-lon4)/2) + Math.Sin(lat3+lat4) *Math.Sin((lon3+lon4)/2) *Math.Sin((lon3-lon4)/2) ,
                                            Math.Cos(lat3)*Math.Cos(lat4)*Math.Sin(lon3-lon4) };
            double abse1Xe2 = Math.Sqrt(Math.Pow(e1Xe2[0], 2) + Math.Pow(e1Xe2[1], 2) + Math.Pow(e1Xe2[2], 2));
            double[] ea = new double[3]{e1Xe2[0] / abse1Xe2, 
                                        e1Xe2[1] / abse1Xe2,
                                        e1Xe2[2] / abse1Xe2};
            double abse3Xe4 = Math.Sqrt(Math.Pow(e3Xe4[0], 2) + Math.Pow(e3Xe4[1], 2) + Math.Pow(e3Xe4[2], 2));
            double[] eb = new double[3]{e3Xe4[0] / abse3Xe4, 
                                        e3Xe4[1] / abse3Xe4,
                                        e3Xe4[2] / abse3Xe4};
            double[] eaXeb = new double[3]{ea[1] *eb[2] -eb[1] *ea[2], 
                                           ea[2] *eb[0] -eb[2] *ea[0], 
                                           ea[0] *eb[1] -ea[1] *eb[0]};

            double lat = Math.Atan2(eaXeb[2], Math.Sqrt(Math.Pow(eaXeb[0], 2) + Math.Pow(eaXeb[1], 2)));
            double lon = Math.Atan2(-eaXeb[1], eaXeb[0]);

            double antilat = lat * -1;
            double antilon = lon + Math.PI;

            //convert back to degrees and invert lon for convention sake
            lat = Conversions.RadiansToDegrees(lat);
            lon = Conversions.RadiansToDegrees(lon) * -1;
            antilat = Conversions.RadiansToDegrees(antilat);
            antilon = Conversions.RadiansToDegrees(antilon) * -1;

            List<Coordinate> results = new List<Coordinate>();

            Coordinate ll = new Coordinate(lat, lon);

            double l1 = CalculateDistance(ll1, ll2);
            double l1ia = CalculateDistance(ll1, ll);
            double l1ib = CalculateDistance(ll2, ll);

            double l2 = CalculateDistance(ll3, ll4);
            double l2ia = CalculateDistance(ll3, ll);
            double l2ib = CalculateDistance(ll4, ll);

            double test1 = l1 - l1ia - l1ib;
            double test2 = l2 - l2ia - l2ib;
            double small = 1e-4;

            if (Math.Abs(test1) < small)// && Math.Abs(test2) < small)
                results.Add(ll);

            ll = new Coordinate(antilat, antilon);

            l1 = CalculateDistance(ll1, ll2);
            l1ia = CalculateDistance(ll1, ll);
            l1ib = CalculateDistance(ll2, ll);

            l2 = CalculateDistance(ll3, ll4);
            l2ia = CalculateDistance(ll3, ll);
            l2ib = CalculateDistance(ll4, ll);

            test1 = l1 - l1ia - l1ib;
            test2 = l2 - l2ia - l2ib;
            small = 1e-4;

            if (Math.Abs(test1) < small)// && Math.Abs(test2) < small)
                results.Add(ll);

            return results;
        }

        //public static double[] CalculateLLFromBearingRange(double[] latlon, double dist, double heading)
        //{
        //    double[] latlon1 = new double[2];
        //    latlon1[0] = latlon[0];
        //    latlon1[1] = latlon[1];
        //    latlon1[0] *= Math.PI / 180;
        //    latlon1[1] *= Math.PI / 180;
        //    heading *= Math.PI / 180;
        //    dist *= Math.PI / (180 * 60);
        //    double lat = Math.Asin(((Math.Sin(latlon1[0]) * Math.Cos(dist)) + (Math.Cos(latlon1[0]) * Math.Sin(dist) * Math.Cos(heading))));
        //    double dlon = Math.Atan2((Math.Sin(heading) * Math.Sin(dist) * Math.Cos(latlon1[0])), Math.Cos(dist) - (Math.Sin(latlon1[0]) * Math.Sin(lat)));
        //    double lon = ((latlon1[1] - dlon + Math.PI) % (Math.PI * 2)) - Math.PI;

        //    return new double[] { lat * 180 / Math.PI, lon * 180 / Math.PI };
        //}

        public static double Mod(double y, double x)
        {
            return y - x * Math.Floor(y / x);
        }



        public static Coordinate CalculateLLFromBearingRange(Coordinate latlon, double distnm, double heading)
        {
            double[] latlon1 = new double[2];
            latlon1[0] = latlon.Latitude;
            latlon1[1] = latlon.Longitude;
            latlon1[0] *= Math.PI / 180;
            latlon1[1] *= Math.PI / 180;
            heading = DegreesToRadians(heading);
            double dist = distnm * Math.PI / (180*60);
            double lat = Math.Asin(Math.Sin(latlon1[0])*Math.Cos(dist)+Math.Cos(latlon1[0])*Math.Sin(dist)*Math.Cos(heading));
            double dlon = Math.Atan2(Math.Sin(heading) * Math.Sin(dist) * Math.Cos(latlon1[0]), Math.Cos(dist) - Math.Sin(latlon1[0]) * Math.Sin(lat));
            double lon = Mod(latlon1[1] + dlon-Math.PI, 2 * Math.PI) - Math.PI;

            return new Coordinate(RadiansToDegrees(lat), RadiansToDegrees(lon));
        }

        public static Coordinate CheckLLExtents(Coordinate latlon)
        {
            Coordinate nc = latlon;
            if (nc.Latitude > 90)
                nc.Latitude -= 180;
            else if (nc.Latitude < -90)
                nc.Latitude += 180;
            if (nc.Longitude > 180)
                nc.Longitude -= 360;
            else if (nc.Longitude < -180)
                nc.Longitude += 360;
            return nc;
        }

        //public static List<double[]> CalculateArcLLs(double[] centerLL, double[] startLL, double[] endLL, double radius, bool MajorArc = false)
        //{
        //    List<double[]> points = new List<double[]>();

        //    centerLL[0] = DegreesToRadians(centerLL[0]);
        //    centerLL[1] = DegreesToRadians(centerLL[1]);
        //    startLL[0] = DegreesToRadians(startLL[0]);
        //    startLL[1] = DegreesToRadians(startLL[1]);
        //    endLL[0] = DegreesToRadians(endLL[0]);
        //    endLL[1] = DegreesToRadians(endLL[1]);
        //    radius = DegreesToRadians(radius);

        //    //decide the bounds of angles...
        //    double angle_max = CalculateTrack(centerLL, endLL);
        //    double angle_min = CalculateTrack(centerLL, startLL);

        //    double inc = 0.03;


        //    if (angle_max - angle_min < -Math.PI)
        //        angle_min -= 2 * Math.PI;

        //    if (angle_max - angle_min > Math.PI)
        //        angle_min += 2 * Math.PI;

        //    if (MajorArc)
        //    {
        //        if (angle_max - angle_min < Math.PI)
        //            angle_max += 2 * Math.PI;
        //    }

        //    if (angle_min < angle_max)
        //    {
        //        for (double a = angle_min; a < angle_max; a += inc)
        //        {
        //            double[] ll = new double[2];
        //            ll[0] = Math.Asin((Math.Sin(centerLL[0]) * Math.Cos(radius)) + (Math.Cos(centerLL[0]) * Math.Sin(radius) * Math.Cos(a)));
        //            if (Math.Cos(ll[0]) == 0)
        //                ll[1] = ll[0];
        //            else
        //                ll[1] = ((centerLL[1] - Math.Asin(Math.Sin(a) * Math.Sin(radius) / Math.Cos(ll[0])) + Math.PI) % (2 * Math.PI)) - Math.PI;
        //            //convert baca to degrees..
        //            ll[0] = ll[0] * 180 / Math.PI;
        //            ll[1] = ll[1] * 180 / Math.PI;
        //            points.Add(ll);
        //        }
        //    }
        //    else
        //    {
        //        for (double a = angle_min; a > angle_max; a -= inc)
        //        {
        //            double[] ll = new double[2];
        //            ll[0] = Math.Asin((Math.Sin(centerLL[0]) * Math.Cos(radius)) + (Math.Cos(centerLL[0]) * Math.Sin(radius) * Math.Cos(a)));
        //            if (Math.Cos(ll[0]) == 0)
        //                ll[1] = ll[0];
        //            else
        //                ll[1] = ((centerLL[1] - Math.Asin(Math.Sin(a) * Math.Sin(radius) / Math.Cos(ll[0])) + Math.PI) % (2 * Math.PI)) - Math.PI;
        //            //convert baca to degrees..
        //            ll[0] = ll[0] * 180 / Math.PI;
        //            ll[1] = ll[1] * 180 / Math.PI;
        //            points.Add(ll);
        //        }
        //    }

        //    return points;
        //}
        public static double CalculatePressureAltitude(float pressure)
        {
            return (1 - Math.Pow((pressure / 1013.25),0.190284)) * 145366.45;
        }

        public static double[] CalculateVector(double angle_deg, double magnitude)
        {
            double[] vec = new double[2];
            double angle_rad = Conversions.DegreesToRadians(angle_deg);
            vec[0] = magnitude * Math.Cos(angle_rad);
            vec[1] = magnitude * Math.Sin(angle_rad);
            return vec;
        }
        public static double CalculateMagnitude(double[] vector)
        {
            return Math.Sqrt(Math.Pow(vector[0], 2) + Math.Pow(vector[1], 2));
        }
        public static double CalculateAngleDeg(double[] vector)
        {
            return Conversions.RadiansToDegrees(Math.Atan2(vector[1], vector[0]));
        }
        public static string ConvertToReadableLatLongDDDMMSS(Coordinate latlon)
        {
            string lat, lon;
            double latd = Math.Abs(latlon.Latitude);
            double deg = Math.Floor(latd);
            double work = (latd - deg)*60;
            double min = Math.Floor(work);
            double work2 = (work - min) * 60;
            double sec = Math.Round(work2);
            lat = String.Format("{0,2:00}", deg) + " " + String.Format("{0,2:00}", min) + " " + String.Format("{0,2:00}", sec);
            lat += latlon.Latitude < 0 ? "S" : "N";

            deg = Math.Floor(Math.Abs(latlon.Longitude));
            work = (Math.Abs(latlon.Longitude) - deg) * 60;
            min = Math.Floor(work);
            work = (work - min) * 60;
            sec = Math.Round(work);
            lon = String.Format("{0,3:000}", deg) + " " + String.Format("{0,2:00}", min) + " " + String.Format("{0,2:00}", sec);
            lon += latlon.Longitude < 0 ? "W" : "E";
            return lat + " " + lon;
        }
        public static string ConvertToFlightplanLatLong(Coordinate latlon)
        {
            string lat, lon;
            double latd = Math.Abs(latlon.Latitude);
            double deg = Math.Floor(latd);
            double work = (latd - deg) * 60;
            double min = Math.Round(work);
            //double work2 = (work - min) * 60;
            //double sec = Math.Round(work2);
            lat = String.Format("{0,2:00}", deg) + String.Format("{0,2:00}", min);
            lat += latlon.Latitude < 0 ? "S" : "N";

            deg = Math.Floor(Math.Abs(latlon.Longitude));
            work = (Math.Abs(latlon.Longitude) - deg) * 60;
            min = Math.Round(work);
            //work = (work - min) * 60;
            //sec = Math.Round(work);
            lon = String.Format("{0,3:000}", deg) + String.Format("{0,2:00}", min);
            lon += latlon.Longitude < 0 ? "W" : "E";
            return lat + lon;
        }
        public static string ConvertToStripLatLong(Coordinate latlon)
        {
            string lat, lon;
            double latd = Math.Abs(latlon.Latitude);
            double deg = Math.Floor(latd);
            lat = String.Format("{0,2:00}", deg);

            deg = Math.Floor(Math.Abs(latlon.Longitude));
            lon = String.Format("{0,3:000}", deg);
            return lat + " " + lon;
        }
        public static string ConvertToReadableLatLongDDDMM(Coordinate latlon)
        {
            string lat, lon;
            double latd = Math.Abs(latlon.Latitude);
            double deg = Math.Floor(latd);
            double work = (latd - deg) * 60;
            double min = Math.Round(work);
            lat = String.Format("{0,2:00}", deg) + " " + String.Format("{0,2:00}", min);
            lat += latlon.Latitude < 0 ? "S" : "N";

            deg = Math.Floor(Math.Abs(latlon.Longitude));
            work = (Math.Abs(latlon.Longitude) - deg) * 60;
            min = Math.Round(work);
            lon = String.Format("{0,3:000}", deg) + " " + String.Format("{0,2:00}", min);
            lon += latlon.Longitude < 0 ? "W" : "E";
            return lat + " " + lon;
        }

        public static int FrequencyToInt(string freq)
        {
            CheckFrequencyValid(freq);

            double f = double.Parse(freq);
            f *= 1000;//remove decimal point.
            f -= 100000;//remove "1"
            return (int)f;
        }

        public static string FrequencyToString(int freq)
        {
            string f = "1" + (freq / 1000.000).ToString("00.0##");
            CheckFrequencyValid(f);
            return f;
        }

        public static bool CheckFrequencyValid(string freq)
        {
            if (!Regex.IsMatch(freq, @"^[1](([1,2]\d)|([3][0-6]))\.(\d$|\d[0,2,5,7]$|\d[0,2,5,7][0,5]$)") && freq!="199.998")
                throw new Exception("Invalid Frequency entered");
            else
                return true;
        }
    }
}
/*
where code goes to die..
original screentoll
//ll[0] = DegreesToRadians(Form1.ScreenCentreLL[0]) - (y / (vscale * Form1.zoom)); ;
//ll[0] = RadiansToDegrees(ll[0]);
//ll[1] = DegreesToRadians(Form1.ScreenCentreLL[1]) + (x / (vscale * Form1.zoom));
//ll[1] = RadiansToDegrees(ll[1]);

original lltoscreen
//x = DegreesToRadians(ll[1]-Form1.ScreenCentreLL[1]) * vscale * Form1.zoom;
            //y = DegreesToRadians(Form1.ScreenCentreLL[0] - ll[0]) * vscale * Form1.zoom;
            //return new double[2]{x,y};
*/