Due to the fact browse_rsc can't create subdirectories, every time you update font-awesome you'll need to change relative webfont references in all.min.css
eg ../webfonts/fa-regular-400.ttf => fa-regular-400.ttf (or whatever you call it in asset datum)

Second change is ripping out file types other than woff and eot(ie8) from the css

Finally, removing brand related css.