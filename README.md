### How flatten work:

if ApplyPerimeterRampCurve true, flattening based on PerimeterRampCurve (accurate slope)

if false PerimeterRampDistance which is Size of gaussian filter

### Changes to how program works:

every method which is connected to road generation starts inside component Road_Generator, reasons:

- easier to merge (unity have problems if something is component in scene)
- faster to start

added gameObject roads (added only to hold meshcollider in new layer)

# To Do
- change road generation in l systems to splines
- fix river
