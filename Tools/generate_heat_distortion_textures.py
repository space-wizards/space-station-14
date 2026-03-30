#This is script that was used to generate textures for heatdistortion

from pyfastnoiselite.pyfastnoiselite import FastNoiseLite, NoiseType, FractalType
from PIL import Image
import math

def generate_noise_image(output_filename="perlin_noise.png"):
    width = 512
    height = 512
    
    noise = FastNoiseLite()
    
    noise.noise_type = NoiseType.NoiseType_Perlin
    noise.fractal_type = FractalType.FractalType_FBm
    noise.fractal_octaves = 4
    noise.frequency = 0.01

    image = Image.new("RGBA", (width, height))
    pixels = image.load()

    for x in range(width):
        for y in range(height):
            value = (noise.get_noise(x, y) + 1.0) / 2.0
            color_val = int(value * 255)
            color_val = max(0, min(255, color_val))
            
            pixels[x, y] = (color_val, color_val, color_val, 255)
    image.save(output_filename)
    print(f"Success! Image exported to: {output_filename}")

def generate_soft_circle_texture(output_filename="soft_circle.png"):
    width = 64
    height = 64
    
    image = Image.new("RGBA", (width, height))
    pixels = image.load()
    
    center_x = width / 2.0
    center_y = height / 2.0
    max_dist = width / 2.0

    for x in range(width):
        for y in range(height):
            dist = math.sqrt((x - center_x)**2 + (y - center_y)**2)
            fade = 1.0 - max(0.0, min(1.0, dist / max_dist))
            alpha_val = fade * fade * (3.0 - 2.0 * fade)
            alpha_byte = int(alpha_val * 255)
            pixels[x, y] = (255, 255, 255, alpha_byte)
    image.save(output_filename)
    print(f"Success! Image exported to: {output_filename}")

if __name__ == "__main__":
    generate_noise_image()
    generate_soft_circle_texture()
