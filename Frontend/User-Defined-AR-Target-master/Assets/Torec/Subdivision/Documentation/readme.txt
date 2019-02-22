Catmull-Clark Mesh Subdivision
-----------------------------------------------------------------

The script implements the Catmull–Clark algorithm of mesh subdivision.
Just use the following procedure on development or at runtime:
public Mesh Subdivide(Mesh mesh, int iterations)

Related links:
    https://en.wikipedia.org/wiki/Catmull–Clark_subdivision_surface

-----------------------------------------------------------------

Asset contents:
    Subdivision
        Assets
            Subdivision.cs          - The main module implementing the Subdivide() procedure.
        Demo
            SubdivisionDemo.cs      - A demo component building (on Reset) an array of meshes 
                                      demonstrating subdivision of a cube with some faces missing.
            SubdivisionDemo.unity   - SubdivisionDemo component work result.

-----------------------------------------------------------------

Viktor Massalogin
massalogin@gmail.com
