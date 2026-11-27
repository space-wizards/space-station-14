--[[

SS14 Displacement Map Visualizer

This script will create a little preview window that will test a displacement map.

TODO: Handling of sizes != 127 doesn't work properly and rounds differently from the real shader. Ah well.

]] --

-- ============================================
-- ERROR MESSAGES
-- ============================================

local error_sprite = "No sprite open!"
local error_cel    = { "Could not find the selected cel!",
    "This might be because the cel is empty. Ensure the selected cel contains content, then try again." }

-- ============================================
-- SCRIPT
-- ============================================

-- This script requires UI
if not app.isUIAvailable then
    return
end

-- the active sprite
local sprite

local function updateSprite()
    sprite = app.sprite
    if not sprite then
        -- throw an error if there's no sprite open
        app.alert(error_sprite)
    end
end
updateSprite()

-- event listener, only active when dialog is open
local spriteChanged = nil

local shiftDlg = nil

local dialog = Dialog {
    title = "Displacement Map Visualizer",
    onclose = function()
        -- unsubscribe from sprite change events
        if spriteChanged then
            if app.events then app.events:off(spriteChanged) end
            if sprite.events then sprite.events:off(spriteChanged) end
        end
        spriteChanged = nil
        if shiftDlg then shiftDlg:close() end
    end
}

-- ============================================
-- LAYER HELPERS
-- ============================================

-- cached layer list
local layers = {}

-- refresh layer comboboxes
local function updateLayerUI()
    pcall(function()
        dialog:modify { id = "displacement-select", options = layers }
        dialog:modify { id = "reference-select", options = layers }
        dialog:modify { id = "background-select", options = layers }
    end)
end

-- refresh cached layers
local function updateLayers()
    layers = {}
    for _, layer in ipairs(sprite.layers) do
        table.insert(layers, 1, layer.name)
    end
end

updateLayers()

-- return the layer with the specified name
local findLayer = function(name)
    if not sprite then return end
    for _, layer in ipairs(sprite.layers) do
        if layer.name == name then
            return layer
        end
    end
    return nil
end

-- returns the first cel of the selected layer
local firstCel = function(layer)
    local out = layer:cel(1)
    if not out then return nil end
    return out
end

-- if a new sprite is opened:
-- update the list of cached layers.
spriteChanged = function()
    updateSprite()
    updateLayers()
    dialog:repaint()
end

if app.events then app.events:on('sitechange', spriteChanged) end
if sprite.events then sprite.events:on('change', spriteChanged) end

-- ============================================
-- OUT OF BOUNDS
-- ============================================

-- are there pixels that will fall out of the sprite bounds?
local hasOobPixels = false

local function isOutOfBounds(x, y, dx, dy)
    local size = dialog.data["frame-size"]

    -- user is probably trying to type a new value
    if size == 0 then return end

    -- I messed around in Desmos for 2 hours trying to find a function that could do all of this at once
    -- but I am sadly not a math major
    -- This works by checking to see if we've wrapped around from say 31 to 01 which indicates that we've gone over
    -- the edges of a sprite's bounds.
    if dx > 0 and math.fmod(x + dx, size) < math.fmod(x, size) then
        return true
    end
    -- gotta add size here in case we go from 0 -> -1, since mod -1 is just -1 not 31
    if dx < 0 and math.fmod(x + size + dx, size) > math.fmod(x, size) then
        return true
    end
    if dy > 0 and math.fmod(y + dy, size) < math.fmod(y, size) then
        return true
    end
    if dy < 0 and math.fmod(y + size + dy, size) > math.fmod(y, size) then
        return true
    end

    return false
end

local function getOobColor(x, y)
    if dialog.data["mark-oob-checkerboard"] then -- requested by Emogarbage :3
        local size = dialog.data["frame-size"]
        if (math.sin(math.pi * x * 8.0 / size) > 0) == (math.cos(math.pi * y * 8.0 / size) > 0) then
            return Color { r = 0, g = 0, b = 0, a = 255 }
        end
    end
    return dialog.data["mark-oob-color"]
end

-- ============================================
-- DISPLACEMENT MATHS
-- ============================================

-- 128 represents 0, 0
-- X axis represented by colour's R level (positive = left)
-- Y axis represented by colour's G level (positive = up)

-- gets the cel's pixel at a specific x and y coordinate
local getOffsetPixel = function(x, y, offset_x, offset_y, target)
    if isOutOfBounds(x, y, offset_x, offset_y) then
        hasOobPixels = true
        if dialog.data["mark-oob"] then
            return getOobColor(x, y)
        end
    end

    local rect = target.bounds
    local posX = x - rect.x
    local posY = y - rect.y

    local image = target.image
    if (image.bounds:contains(Rectangle(posX + offset_x, posY + offset_y, 1, 1))) then
        return image:getPixel(posX + offset_x, posY + offset_y)
    end

    return image.spec.transparentColor
end

-- draws the target image with displacement map applied
local applyDisplacementMap = function(displacement, target)
    local width = sprite.width
    local height = sprite.height

    -- generate a blank canvas that's the same size as our target
    local image = target.image:clone()
    image:resize(width, height)
    image:clear()

    -- local size = dialog.data["displacement-size"] TODO SIZE
    local size = 127

    for x = 0, width - 1 do
        for y = 0, height - 1 do
            if not displacement.bounds:contains(Rectangle { x, y, 1, 1 }) then
                goto continue
            end

            local value = displacement.image:getPixel(
                x - displacement.bounds.x,
                y - displacement.bounds.y)
            local color = Color(value)

            if color.alpha == 0 then
                goto continue
            end

            local offset_x = (color.red - 128) / 127 * size
            local offset_y = (color.green - 128) / 127 * size

            local colorValue = getOffsetPixel(x, y, offset_x, offset_y, target)

            image:drawPixel(x, y, colorValue)

            ::continue::
        end
    end
    return image
end

-- ============================================
-- SHIFT HELPERS
-- ============================================

local function clamp(x, min, max)
    if x < min then return min end
    if x > max then return max end
    return x
end

-- checks if this pixel is in the requested selection.
-- if so, returns the pixel colour.
local function tryGetColour(pixel, selection)
    local pos = Point(pixel.x, pixel.y)
    local color = Color(pixel())

    if not app.cel then return nil end
    if not selection:contains(pos.x + app.cel.position.x, pos.y + app.cel.position.y) then return nil end

    return color
end

-- returns either selected area, or entire canvas if nothing selected
local function getSelection()
    if sprite.selection.isEmpty then
        sprite.selection:selectAll()
    end
    return sprite.selection
end


-- ============================================
-- OFFSET, FLIP, BLUE (SHIFT UI)
-- ============================================

local function openShiftDialog()
    shiftDlg = Dialog {
        title = "Shift Selection",
        onclose = function()
            shiftDlg = nil
        end
    }

    -- returns target cel and a clone of the cel's image
    local function getClone()
        local cel = app.cel
        if not cel then return nil end
        local image = cel.image:clone()

        return cel, image
    end

    -- the colour's B level doesnt affect anything, so we can use it as a guideline.
    local bFactor = 0

    -- modify blue factor with respect to slider
    local function changeBFactor(value)
        bFactor = math.floor(value)
        shiftDlg:modify { id = "blueSlider", value = bFactor }
    end

    -- apply an RGB offset to the selection
    local shift = function(dx, dy, dz)
        local cel, image = getClone()
        if not cel or not image then
            app.alert { text = error_cel }
            return
        end
        local selection = getSelection()

        for p in image:pixels(selection) do
            local color = tryGetColour(p, selection)
            if not color then goto continue end

            dz = dz or 0

            color.red = clamp(color.red + dx, 0, 255)
            color.green = clamp(color.green + dy, 0, 255)
            color.blue = clamp(color.blue + dz, 0, 255)

            p(color.rgbaPixel)
            ::continue::
        end
        cel.image = image
        app:refresh()
    end

    local function flip(horizontal, vertical)
        local cel, image = getClone()
        if not cel or not image then
            app.alert { text = error_cel }
            return
        end
        local selection = getSelection()

        for p in image:pixels(selection) do
            local color = tryGetColour(p, selection)
            if not color then goto continue end

            if horizontal then
                color.red = 128 + -(color.red - 128)
            end
            if vertical then
                color.green = 128 + -(color.green - 128)
            end

            p(color.rgbaPixel)

            ::continue::
        end
        cel.image = image
        app:refresh()
    end

    local function flipHorizontal()
        flip(true, false)
    end

    local function flipVertical()
        flip(false, true)
    end

    -- apply an exponential RGB offset to the selection
    -- eg. 1 px = 1(bFactor), 2px = 2(bFactor), etc
    local function applyBlue(blue)
        local cel, image = getClone()
        if not cel or not image then
            app.alert { text = error_cel }
            return
        end
        local selection = getSelection()

        for p in image:pixels(selection) do
            local color = tryGetColour(p, selection)
            if not color then goto continue end

            -- get the difference of 128 on both axes
            local offset = math.abs(color.red - 128)
            offset = offset + math.abs(color.green - 128)
            -- determine how much blue this pixel will have
            color.blue = math.min(255, offset * blue)

            p(color.rgbaPixel)

            ::continue::
        end
        cel.image = image
        app:refresh()
    end

    -- ============================================
    -- SHIFT UI
    -- ============================================

    shiftDlg:button {
        text = "↑",
        onclick = function()
            shift(0, 1, bFactor)
        end
    }
    shiftDlg:newrow()
    shiftDlg:button {
        text = "←",
        onclick = function()
            shift(1, 0, bFactor)
        end
    }
    shiftDlg:button {
        text = "→",
        onclick = function()
            shift(-1, -0, bFactor)
        end
    }
    shiftDlg:newrow()
    shiftDlg:button {
        text = "↓",
        onclick = function()
            shift(0, -1, bFactor)
        end
    }

    shiftDlg:newrow {}

    shiftDlg:button {
        id = "flipHorizontal",
        text = "Flip Horizontal",
        onclick = flipHorizontal
    }
    shiftDlg:button {
        id = "flipVertical",
        text = "Flip Vertical",
        onclick = flipVertical
    }

    shiftDlg:slider {
        id = "blueSlider",
        label = "Blue per pixel: ",
        min = 0,
        max = 80,
        value = 0,
        onchange = function()
            local value = shiftDlg.data["blueSlider"]
            changeBFactor(value)
        end
    }

    shiftDlg:button {
        id = "blueButton",
        text = "Blue-ify Selection",
        onclick = function()
            applyBlue(bFactor)
        end
    }
    shiftDlg:button {
        id = "resetBlueButton",
        text = "Remove Selection Blue",
        onclick = function()
            applyBlue(0)
        end
    }

    shiftDlg:show {
        wait = false,
        -- show offset from main window
        bounds = Rectangle(shiftDlg.bounds.x + 175, shiftDlg.bounds.y, shiftDlg.bounds.width, shiftDlg.bounds.height)
    }

    return shiftDlg
end


-- ============================================
-- PREVIEW
-- ============================================

-- scale to view sprites at
local scale = 4

dialog:canvas {
    id = "canvas",
    width = sprite.width * scale,
    height = sprite.height * scale,
    onpaint = function(ev)
        -- get graphics context
        local gc = ev.context
        hasOobPixels = false

        -- find our layers
        local layerDisplacement = findLayer(dialog.data["displacement-select"])
        local layerTarget = findLayer(dialog.data["reference-select"])
        local layerBackground = findLayer(dialog.data["background-select"])

        if not layerDisplacement or not layerTarget or not layerBackground then return end

        -- always use the first cel.
        local celDisplacement = firstCel(layerDisplacement)
        local celTarget = firstCel(layerTarget)
        local celBackground = firstCel(layerBackground)

        if not celDisplacement or not celTarget or not celBackground then
            -- debug error
            --app.alert { text = error_cel }
            return
        end

        -- source and dist rectangles for bg
        local bgSrcRect = Rectangle(
            0, 0,
            celBackground.image.width,
            celBackground.image.height
        )
        local bgDistRect = Rectangle(
            celBackground.position.x * scale,
            celBackground.position.y * scale,
            celBackground.image.width * scale,
            celBackground.image.height * scale
        )

        -- Draw background
        gc:drawImage(celBackground.image, bgSrcRect, bgDistRect)

        -- Apply displacement map and draw
        local image = applyDisplacementMap(celDisplacement, celTarget)

        -- source and dist rectangles for displacement
        local displacementSrcRect = Rectangle(
            0, 0,
            image.width, image.height)
        local displacementDistRect = Rectangle(
            0, 0,
            image.width * scale, image.height * scale)

        gc:drawImage(image, displacementSrcRect, displacementDistRect)
        dialog:modify {
            id = "oob-pixels-warn",
            visible = hasOobPixels
        }
    end
}

-- ============================================
-- REMAINDER OF THE UI
-- ============================================

dialog:separator {}

dialog:combobox {
    id = "displacement-select",
    label = "Displacement Map:",
    options = layers,
    onchange = function()
        dialog:repaint()
    end
}

dialog:combobox {
    id = "reference-select",
    label = "Layer to Displace:",
    options = layers,
    onchange = function()
        dialog:repaint()
    end
}

dialog:combobox {
    id = "background-select",
    label = "Background:",
    options = layers,
    onchange = function()
        dialog:repaint()
    end
}

dialog:button {
    text = "Refresh Layers",
    onclick = updateLayerUI
}

-- dialog:slider {
--     id = "displacement-size",
--     label = "displacement size",
--     min = 127, --We dont support non 127 atm
--     max = 127,
--     value = 127,
--     onchange = function()
--         dialog:repaint()
--     end
-- }

-- Out of Bounds marking
dialog:separator {}

dialog:label {
    id = "oob-pixels-warn",
    text = "Warning: Out-of-bounds displacements detected!",
    visible = false
}

dialog:check {
    id = "mark-oob",
    label = "Mark Out-of-Bounds Displacements",
    selected = false,
    hexpand = false,
    onclick = function()
        dialog:repaint()
    end
}

dialog:check {
    id = "mark-oob-checkerboard",
    label = "Checkerboard Pattern",
    selected = false,
    hexpand = false,
    onclick = function()
        dialog:repaint()
    end
}

dialog:number {
    id = "frame-size",
    label = "Frame Size",
    text = "32",
    hexpand = false,
    onchange = function()
        dialog:repaint()
    end
}

dialog:color {
    id = "mark-oob-color",
    label = "Out-of-Bounds Pixels Color",
    color = Color { r = 255, g = 0, b = 0 },
    onchange = function()
        dialog:repaint()
    end
}

dialog:separator {}

dialog:button {
    text = "Shift Menu",
    onclick = function()
        shiftDlg = openShiftDialog()
    end
}

dialog:button {
    id = "save-displaced",
    text = "Copy Displaced Sprite To New Canvas",
    onclick = function()
        local layerDisplacement = findLayer(dialog.data["displacement-select"])
        local layerTarget = findLayer(dialog.data["reference-select"])
        if not layerDisplacement or not layerTarget then
            app.alert("Select both displacement and reference layers!")
            return
        end

        local celDisplacement = layerDisplacement:cel(1)
        local celTarget = layerTarget:cel(1)
        if not celDisplacement or not celTarget then
            app.alert { text = error_cel }
            return
        end

        local displacedImage = applyDisplacementMap(celDisplacement, celTarget)

        local newSprite = Sprite(sprite.width, sprite.height, sprite.colorMode)
        if sprite.colorMode == ColorMode.INDEXED then
            for i = 0, sprite.palettes[1].size - 1 do
                newSprite.palettes[1]:setColor(i, sprite.palettes[1]:getColor(i))
            end
        end

        local newLayer = newSprite.layers[1]
        local newCel = firstCel(newLayer)
        if not newCel then return end

        newCel.image:drawImage(displacedImage, 0, 0)
        app.activeSprite = newSprite
    end
}

openShiftDialog()
dialog:show {
    wait = false,
    -- show offset from shift window
    bounds = Rectangle(dialog.bounds.x - 175, dialog.bounds.y, dialog.bounds.width, dialog.bounds.height)
}
dialog:repaint()
