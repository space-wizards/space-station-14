-- Displacement Map Visualizer
--
-- This script will create a little preview window that will test a displacement map.
--
-- TODO: Handling of sizes != 127 doesn't work properly and rounds differently from the real shader. Ah well.

local scale = 4
local hasOobPixels = false

-- This script requires UI
if not app.isUIAvailable then
    return
end

local sprite = app.editor.sprite

local spriteChanged = sprite.events:on("change",
    function()
        dialog:repaint()
    end
)

dialog = Dialog{
    title = "Displacement map preview",
    onclose = function(ev)
        sprite.events:off(spriteChanged)
    end
}

function isOutOfBounds(x,y, dx, dy)
    local size = dialog.data["frame-size"]
    -- I messed around in Desmos for 2 hours trying to find a function that could do all of this at once
    -- but I am sadly not a math major
    -- This works by checking to see if we've wrapped around from say 31 to 01 which indicates that we've gone over
    -- the edges of a sprite's bounds.
    if dx > 0 and math.fmod(x+dx, size) < math.fmod(x, size) then
        return true
    end
    -- gotta add size here in case we go from 0 -> -1, since mod -1 is just -1 not 31
    if dx < 0 and math.fmod(x+size+dx, size) > math.fmod(x, size) then
        return true
    end
    if dy > 0 and math.fmod(y+dy, size) < math.fmod(y, size) then
        return true
    end
    if dy < 0 and math.fmod(y+size+dy, size) > math.fmod(y, size) then
        return true
    end

    return false
end

function getOobColor(x,y)
    if dialog.data["mark-oob-checkerboard"] then -- requested by Emogarbage :3
        local size = dialog.data["frame-size"]
        if (math.sin(math.pi*x*8.0/size) > 0) == (math.cos(math.pi*y*8.0/size) > 0) then
            return Color{r=0, g=0, b=0, a=255}
        end
    end
    return dialog.data["mark-oob-color"]
end

function getOffsetPixel(x, y, dx, dy, image, bounds)
    if isOutOfBounds(x,y,dx,dy,image) then
        hasOobPixels = true
        if dialog.data["mark-oob"] then
            return getOobColor(x,y)
        end
    end
    local adj_x = x - bounds.x
    local adj_y = y - bounds.y

    if (image.bounds:contains(Rectangle{adj_x+dx, adj_y+dy, 1, 1})) then
        return image:getPixel(adj_x+dx, adj_y+dy)
    end

    return image.spec.transparentColor
end



function applyDisplacementMap(width, height, displacement, target)
    local image = target.image:clone()
    image:resize(width, height)
    image:clear()

    local displacement_size = dialog.data["displacement_size"]

    for x = 0, width - 1 do
        for y = 0, height - 1 do
            if not displacement.bounds:contains(Rectangle{x,y,1,1}) then
                goto continue
            end

            local color = Color(displacement.image:getPixel(x - displacement.bounds.x,y - displacement.bounds.y))

            if color.alpha == 0 then
                goto continue
            end

            local dx = (color.red - 128) / 127 * displacement_size
            local dy = (color.green - 128) / 127 * displacement_size

            local colorValue = getOffsetPixel(x, y, dx, dy, target.image, target.bounds)
            image:drawPixel(x, y, colorValue)

            ::continue::
        end
    end
    return image
end


local layers = {}
for i,layer in ipairs(sprite.layers) do
    table.insert(layers, 1, layer.name)
end

function findLayer(_sprite, name)
    for i,layer in ipairs(_sprite.layers) do
        if layer.name == name then
            return layer
        end
    end
    return nil
end

dialog:canvas{
    id = "canvas",
    width = sprite.width * scale,
    height = sprite.height * scale,
    onpaint = function(ev)
        local context = ev.context
        hasOobPixels = false

        local layerDisplacement = findLayer(sprite, dialog.data["displacement-select"])
        local layerTarget = findLayer(sprite, dialog.data["reference-select"])
        local layerBackground = findLayer(sprite, dialog.data["background-select"])
        -- print(layerDisplacement.name)
        -- print(layerTarget.name)

        local celDisplacement = layerDisplacement:cel(1)
        local celTarget = layerTarget:cel(1)
        local celBackground = layerBackground:cel(1)

        -- Draw background
        context:drawImage(
            -- srcImage
            celBackground.image,
            -- srcPos
            0, 0,
            -- srcSize
            celBackground.image.width, celBackground.image.height,
            -- dstPos
            celBackground.position.x * scale, celBackground.position.y * scale,
            -- dstSize
            celBackground.image.width * scale, celBackground.image.height * scale)

        -- Apply displacement map and draw
        local image = applyDisplacementMap(
            sprite.width, sprite.height,
            celDisplacement,
            celTarget)

        context:drawImage(
            -- srcImage
            image,
            -- srcPos
            0, 0,
            -- srcSize
            image.width, image.height,
            -- dstPos
            0, 0,
            -- dstSize
            image.width * scale, image.height * scale)
        dialog:modify{
            id = "oob-pixels-warn",
            visible = hasOobPixels
        }
    end
}

dialog:combobox{
    id = "displacement-select",
    label = "displacement layer",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:combobox{
    id = "reference-select",
    label = "reference layer",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:combobox{
    id = "background-select",
    label = "background layer",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:slider{
    id = "displacement_size",
    label = "displacement size",
    min = 127, --We dont support non 127 atm
    max = 127,
    value = 127,
    onchange = function(ev)
        dialog:repaint()
    end
}

-- Out of Bounds marking
dialog:separator()

dialog:label{
    id = "oob-pixels-warn",
    text = "Warning: Out-of-bounds displacements detected!",
    visible = false
}

dialog:check{
    id = "mark-oob",
    label = "Mark Out-of-Bounds Displacements",
    selected = false,
    hexpand = false,
    onclick = function(ev)
        dialog:repaint()
    end
}

dialog:check{
    id = "mark-oob-checkerboard",
    label = "Checkerboard Pattern",
    selected = false,
    hexpand = false,
    onclick = function(ev)
        dialog:repaint()
    end
}

dialog:number{
    id = "frame-size",
    label = "Frame Size",
    text = "32",
    hexpand = false,
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:color{
    id = "mark-oob-color",
    label = "Out-of-Bounds Pixels Color",
    color = Color{r = 255, g = 0, b = 0},
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:show{wait = false}
