using Content.Shared.Item;
using Content.Shared.Storage;

namespace Content.Shared.Nyanotrasen.Item.PseudoItem;

/// <summary>
/// Almost all of this is code taken from other systems, but adapted to use PseudoItem.
/// I couldn't use the original functions because the resolve would fuck shit up, even if I passed a constructed itemcomp
///
/// This is horrible, and I hate it. But such is life
/// </summary>
public partial class SharedPseudoItemSystem
{
    protected bool CheckItemFits(Entity<PseudoItemComponent?> itemEnt, Entity<StorageComponent?> storageEnt)
    {
        if (!Resolve(itemEnt, ref itemEnt.Comp) || !Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        if (Transform(itemEnt).Anchored)
            return false;

        if (storageEnt.Comp.Whitelist?.IsValid(itemEnt, EntityManager) == false)
            return false;

        if (storageEnt.Comp.Blacklist?.IsValid(itemEnt, EntityManager) == true)
            return false;

        var maxSize = _storage.GetMaxItemSize(storageEnt);
        if (_item.GetSizePrototype(itemEnt.Comp.Size) > maxSize)
            return false;

        // The following is shitfucked together straight from TryGetAvailableGridSpace, but eh, it works

        var itemComp = new ItemComponent
            { Size = itemEnt.Comp.Size, Shape = itemEnt.Comp.Shape, StoredOffset = itemEnt.Comp.StoredOffset };

        var storageBounding = storageEnt.Comp.Grid.GetBoundingBox();

        Angle startAngle;
        if (storageEnt.Comp.DefaultStorageOrientation == null)
            startAngle = Angle.FromDegrees(-itemComp.StoredRotation); // PseudoItem doesn't support this
        else
        {
            if (storageBounding.Width < storageBounding.Height)
            {
                startAngle = storageEnt.Comp.DefaultStorageOrientation == StorageDefaultOrientation.Horizontal
                    ? Angle.Zero
                    : Angle.FromDegrees(90);
            }
            else
            {
                startAngle = storageEnt.Comp.DefaultStorageOrientation == StorageDefaultOrientation.Vertical
                    ? Angle.Zero
                    : Angle.FromDegrees(90);
            }
        }

        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                for (var angle = startAngle; angle <= Angle.FromDegrees(360 - startAngle); angle += Math.PI / 2f)
                {
                    var location = new ItemStorageLocation(angle, (x, y));
                    if (ItemFitsInGridLocation(itemEnt, storageEnt, location.Position, location.Rotation))
                        return true;
                }
            }
        }

        return false;
    }

    private bool ItemFitsInGridLocation(
        Entity<PseudoItemComponent?> itemEnt,
        Entity<StorageComponent?> storageEnt,
        Vector2i position,
        Angle rotation)
    {
        if (!Resolve(itemEnt, ref itemEnt.Comp) || !Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        var gridBounds = storageEnt.Comp.Grid.GetBoundingBox();
        if (!gridBounds.Contains(position))
            return false;

        var itemShape = GetAdjustedItemShape(itemEnt, rotation, position);

        foreach (var box in itemShape)
        {
            for (var offsetY = box.Bottom; offsetY <= box.Top; offsetY++)
            {
                for (var offsetX = box.Left; offsetX <= box.Right; offsetX++)
                {
                    var pos = (offsetX, offsetY);

                    if (!IsGridSpaceEmpty(itemEnt, storageEnt, pos, itemShape))
                        return false;
                }
            }
        }

        return true;
    }

    private IReadOnlyList<Box2i> GetAdjustedItemShape(Entity<PseudoItemComponent?> entity, Angle rotation,
        Vector2i position)
    {
        if (!Resolve(entity, ref entity.Comp))
            return new Box2i[] { };

        var shapes = entity.Comp.Shape ?? _item.GetSizePrototype(entity.Comp.Size).DefaultShape;
        var boundingShape = shapes.GetBoundingBox();
        var boundingCenter = ((Box2) boundingShape).Center;
        var matty = Matrix3.CreateTransform(boundingCenter, rotation);
        var drift = boundingShape.BottomLeft - matty.TransformBox(boundingShape).BottomLeft;

        var adjustedShapes = new List<Box2i>();
        foreach (var shape in shapes)
        {
            var transformed = matty.TransformBox(shape).Translated(drift);
            var floored = new Box2i(transformed.BottomLeft.Floored(), transformed.TopRight.Floored());
            var translated = floored.Translated(position);

            adjustedShapes.Add(translated);
        }

        return adjustedShapes;
    }

    private bool IsGridSpaceEmpty(Entity<PseudoItemComponent?> itemEnt, Entity<StorageComponent?> storageEnt,
        Vector2i location, IReadOnlyList<Box2i> shape)
    {
        if (!Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        var validGrid = false;
        foreach (var grid in storageEnt.Comp.Grid)
        {
            if (grid.Contains(location))
            {
                validGrid = true;
                break;
            }
        }

        if (!validGrid)
            return false;

        foreach (var (ent, storedItem) in storageEnt.Comp.StoredItems)
        {
            if (ent == itemEnt.Owner)
                continue;

            var adjustedShape = shape;
            foreach (var box in adjustedShape)
            {
                if (box.Contains(location))
                    return false;
            }
        }

        return true;
    }
}
