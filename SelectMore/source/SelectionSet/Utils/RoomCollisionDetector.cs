using Autodesk.Revit.DB.Architecture;

namespace SelectionSet.Utils
{
    public enum ContactType
    {
        None,
        SurfaceContact,
        VolumetricCollision
    }

    public static class RoomCollisionDetector
    {
        private const double TOLERANCE = 0.001;

        public static Dictionary<Element, ContactType> GetElementsInContactWithRoom(this Room room)
        {
            var doc = room.Document;
            Dictionary<Element, ContactType> results = new Dictionary<Element, ContactType>();

            if (room == null || room.Location == null)
                return results;

            // Obtain room geometry
            Options geoOptions = new Options();
            geoOptions.ComputeReferences = true;
            geoOptions.DetailLevel = ViewDetailLevel.Fine;

            GeometryElement roomGeom = room.get_Geometry(geoOptions);
            if (roomGeom == null)
                return results;

            List<Solid> roomSolids = ExtractSolids(roomGeom);
            List<Face> roomFaces = ExtractFaces(roomSolids);

            if (roomSolids.Count == 0)
                return results;

            BoundingBoxXYZ roomBB = room.get_BoundingBox(null);
            XYZ expansion = new XYZ(0.5, 0.5, 0.5);
            Outline outline = new Outline(roomBB.Min - expansion, roomBB.Max + expansion);
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector = new FilteredElementCollector(doc)
            .WherePasses(bbFilter)
            .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                if (elem.Id == room.Id || elem is Room)
                    continue;

                GeometryElement elemGeom = elem.get_Geometry(geoOptions);
                if (elemGeom == null)
                    continue;

                List<Solid> elemSolids = ExtractSolids(elemGeom);
                if (elemSolids.Count == 0)
                    continue;

                ContactType contactType = AnalyzeContact(roomSolids, roomFaces, elemSolids);

                if (contactType != ContactType.None)
                {
                    results[elem] = contactType;
                }
            }

            return results;
        }

        private static ContactType AnalyzeContact(List<Solid> roomSolids,
        List<Face> roomFaces, List<Solid> elemSolids)
        {
            bool hasSurfaceContact = false;

            foreach (Solid roomSolid in roomSolids)
            {
                foreach (Solid elemSolid in elemSolids)
                {
                    if (roomSolid.Volume < TOLERANCE || elemSolid.Volume < TOLERANCE)
                        continue;

                    try
                    {
                        Solid intersection = BooleanOperationsUtils
                        .ExecuteBooleanOperation(roomSolid, elemSolid,
                        BooleanOperationsType.Intersect);

                        if (intersection != null && intersection.Volume > TOLERANCE)
                        {
                            return ContactType.VolumetricCollision;
                        }
                    }
                    catch
                    {

                    }

                    if (!hasSurfaceContact)
                    {
                        List<Face> elemFaces = ExtractFaces(new List<Solid> { elemSolid });

                        foreach (Face roomFace in roomFaces)
                        {
                            foreach (Face elemFace in elemFaces)
                            {
                                if (AreFacesTouching(roomFace, elemFace))
                                {
                                    hasSurfaceContact = true;
                                    break;
                                }
                            }
                            if (hasSurfaceContact)
                                break;
                        }
                    }
                }
            }

            if (hasSurfaceContact)
                return ContactType.SurfaceContact;

            return ContactType.None;
        }

        private static bool AreFacesTouching(Face face1, Face face2)
        {
            try
            {
                List<XYZ> samplePoints1 = GetSamplePointsFromFace(face1, 5);
                List<XYZ> samplePoints2 = GetSamplePointsFromFace(face2, 5);

                if (samplePoints1.Count == 0 || samplePoints2.Count == 0)
                    return false;

                int closePointsCount = 0;

                foreach (XYZ point1 in samplePoints1)
                {
                    IntersectionResult result = face2.Project(point1);
                    if (result != null && result.Distance < TOLERANCE)
                    {
                        closePointsCount++;
                    }
                }

                if (closePointsCount >= 2)
                    return true;

                closePointsCount = 0;
                foreach (XYZ point2 in samplePoints2)
                {
                    IntersectionResult result = face1.Project(point2);
                    if (result != null && result.Distance < TOLERANCE)
                    {
                        closePointsCount++;
                    }
                }

                return closePointsCount >= 2;
            }
            catch
            {
                return false;
            }
        }

        private static List<XYZ> GetSamplePointsFromFace(Face face, int sampleCount)
        {
            List<XYZ> points = new List<XYZ>();

            try
            {
                BoundingBoxUV bbox = face.GetBoundingBox();
                double uMin = bbox.Min.U;
                double uMax = bbox.Max.U;
                double vMin = bbox.Min.V;
                double vMax = bbox.Max.V;

                for (int i = 0; i < sampleCount; i++)
                {
                    for (int j = 0; j < sampleCount; j++)
                    {
                        double u = uMin + (uMax - uMin) * i / (sampleCount - 1);
                        double v = vMin + (vMax - vMin) * j / (sampleCount - 1);

                        try
                        {
                            XYZ point = face.Evaluate(new UV(u, v));
                            points.Add(point);
                        }
                        catch
                        {

                        }
                    }
                }

                if (points.Count > 0)
                {
                    double uCenter = (uMin + uMax) / 2;
                    double vCenter = (vMin + vMax) / 2;
                    try
                    {
                        XYZ center = face.Evaluate(new UV(uCenter, vCenter));
                        points.Add(center);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }

            return points;
        }

        private static List<Solid> ExtractSolids(GeometryElement geomElem)
        {
            List<Solid> solids = new List<Solid>();

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid && solid.Volume > TOLERANCE)
                {
                    solids.Add(solid);
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    solids.AddRange(ExtractSolids(instGeom));
                }
            }

            return solids;
        }

        private static List<Face> ExtractFaces(List<Solid> solids)
        {
            List<Face> faces = new List<Face>();

            foreach (Solid solid in solids)
            {
                if (solid == null || solid.Volume < TOLERANCE)
                    continue;

                foreach (Face face in solid.Faces)
                {
                    if (face != null && face.Area > TOLERANCE)
                    {
                        faces.Add(face);
                    }
                }
            }

            return faces;
        }
    }
}
