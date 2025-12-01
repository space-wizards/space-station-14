local sprite = app.editor.sprite
local cel = app.cel

function Shift(dx, dy)
    if sprite.selection.isEmpty then
        sprite.selection:selectAll()
    end

    local selection = sprite.selection
    local image = cel.image:clone()

    for it in image:pixels(selection) do
        local color = Color(it())
        local position = Point(it.x, it.y) -- gets the position

        if not selection:contains(position.x + cel.position.x, position.y + cel.position.y) then
            goto continue
        end

        color.red = math.min(255, math.max(0, color.red + dx))
        color.green = math.min(255, math.max(0, color.green + dy))

        it(color.rgbaPixel)

        ::continue::
    end
    cel.image = image
    app.refresh()
end

local diag = Dialog{
    title = "Shift Displacement Map"
}

diag
    :button{
        text="↑",
        onclick=function()
            Shift(0,1)
        end
    }
    :newrow()
    :button{
        text="←",
        onclick=function()
            Shift(1,0)
        end
    }
    :button{
        text="→",
        onclick=function()
            Shift(-1,0)
        end
    }
    :newrow()
    :button{
        text="↓",
        onclick=function()
            Shift(0,-1)
        end
    }

diag:show{wait=false}
