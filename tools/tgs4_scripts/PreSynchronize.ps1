param(
    $game_path
)

cd $game_path

Write-Host "Installing pip dependencies..."
pip3 install PyYaml beautifulsoup4
if(!$?){
    Write-Host "pip3 returned non-zero!"
    exit $LASTEXITCODE
}

Write-Host "Running changelog script..."
python3 tools/ss13_genchangelog.py html/changelog.html html/changelogs
if(!$?){
    Write-Host "python3 returned non-zero!"
    exit $LASTEXITCODE
}

Write-Host "Committing changes..."
git add html

if(!$?){
    Write-Host "`git add` returned non-zero!"
    exit $LASTEXITCODE
}

#we now don't care about failures
git commit -m "Automatic changelog compile, [ci skip]"
exit 0
