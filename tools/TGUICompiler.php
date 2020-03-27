<?php
//Tool for web based TGUI compilation
//by Cyberboss

//RESTRICTIONS
//Restricted to Windows for now because it uses mklink cmd function

//INSTALLATION
//Setup PHP
//Place this file in it's own directory and rewrite all requests to the directory to this file
//add extension=php_fileinfo.dll to php.ini
//ensure fastcgi.impersonate is set to 0 in php.ini
//clone a tgui repository and place next to this file
//run install_dependencies.bat
//MOVE (not copy) the node_modules folder next to this file
try{
	//CONFIG
	$repo_dir = 'tgstation';
	$path_to_tgui_from_repo = '/tgui';
	$full_path_to_gulp = 'C:/Users/Cyberboss/AppData/Roaming/npm/gulp';	//this needs to be read/executable by the PHP app pool
	$max_number_of_uploads = 20;
	//END CONFIG

	function getGitRevision()
	{
		global $tgdir;
		$rev = trim(file_get_contents($tgdir . '/.git/HEAD'));

		if (substr($rev, 0, 4) == 'ref:') {
			$tmp = explode('/', $rev);
			$ref = end($tmp);
			$rev = trim(file_get_contents($tgdir . "/.git/refs/heads/{$ref}"));
		}

		return $rev;
	}

	function extrapolate_git_url(){
		global $tgdir;
		$config = file($tgdir . '/.git/config', FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
		foreach($config as $line)
			if(strpos($line, 'url = ') !== false)
				return trim(explode('=', $line)[1]);
	}

	function download_file($path){
		header('Content-type: application/zip'); 
		header('Content-Disposition: attachment; filename=' . basename($path));
		header('Content-length: ' . filesize($path));
		header('Pragma: no-cache'); 
		header('Expires: 0'); 
		readfile($path);
	}

	function recurse_copy($src,$dst) { 
		$dir = opendir($src); 
		@mkdir($dst); 
		while(false !== ( $file = readdir($dir)) ) { 
			if (( $file != '.' ) && ( $file != '..' )) { 
				if ( is_dir($src . '/' . $file) ) { 
					recurse_copy($src . '/' . $file,$dst . '/' . $file); 
				} 
				else { 
					copy($src . '/' . $file,$dst . '/' . $file); 
				} 
			} 
		} 
		closedir($dir); 
	} 

	function rrmdir($dir) { 
		if (is_dir($dir)) { 
		$objects = scandir($dir); 
		foreach ($objects as $object) { 
			if ($object != "." && $object != "..") { 
			if (is_dir($dir."/".$object))
				rrmdir($dir."/".$object);
			else
				unlink($dir."/".$object); 
			} 
		}
		rmdir($dir); 
		} 
	}

	function update_git(){
		global $tgdir;
		shell_exec('cd ' . $tgdir . ' && git pull');
	}
	
	$full_path_to_gulp = str_replace('/', '\\', $full_path_to_gulp);
	$parent_dir = str_replace('\\', '/', realpath(dirname(__FILE__)));
	$tgdir = $parent_dir . '/' . $repo_dir;

	$revision = getGitRevision();
	$git_base = extrapolate_git_url();
	if($git_base)
		$commit_url = $git_base . '/commit/' . $revision;
	else
		$error = 'Unable to determine github URL!';

	if($_SERVER['REQUEST_METHOD'] === 'POST'){
		$updated_git = isset($_POST['pull']);
		if($updated_git){
			update_git();
			$revision = getGitRevision();
		}
		$good_files = array();
		$finfo = new finfo(FILEINFO_MIME_TYPE);
		foreach($_FILES as $F){
			$name = $F['name'];
			if(isset($F['error']) && !is_array($F['error']) && $F['error'] == UPLOAD_ERR_OK && $F['size'] > 0 && strpos($name, '\\') == false && strpos($name, '/') == false){
				$ext = pathinfo($name, PATHINFO_EXTENSION);
				$mime = $finfo->file($F['tmp_name']);
				if($ext == 'ract' && $mime == 'text/plain')
					$good_files[] = $F;
			}
		}
		$the_count = count($good_files);
		if($the_count > 0 && $the_count < $max_number_of_uploads){
			$tgtgui_path = $tgdir . $path_to_tgui_from_repo;
			$requests_dir = $parent_dir . '/requests';
			if(!is_dir($requests_dir))
				mkdir($requests_dir);
			$target_path = str_replace('\\', '/', tempnam($requests_dir, 'tgui'));
			unlink($target_path);
			recurse_copy($tgtgui_path, $target_path);
			$parent_node = $parent_dir . '/node_modules';
			$target_node = $target_path . '/node_modules';
			exec('mklink /j "' . str_replace('/', '\\', $target_node) . '" "' . str_replace('/', '\\', $parent_node) . '"');

			//now copy the uploads to the thing
			$target_interfaces = $target_path . '/src/interfaces/';
			foreach($good_files as $F){
				$target_name = $target_interfaces . $F['name'];
				if(file_exists($target_name))
					unlink($target_name); //remove the file
				move_uploaded_file($F['tmp_name'], $target_name);
			}
			
			//compile
			$command = '"' . $full_path_to_gulp . '" --cwd "' . str_replace('/', '\\', $target_path) . '" --min 2>&1';
			$output = shell_exec($command);

			$zip = new ZipArchive();
			$zippath = $target_path . '/TGUI.zip';
			if($zip->open($zippath, ZipArchive::CREATE) == TRUE){
				$zip->addFile($target_path . '/assets/tgui.css', 'tgui.css');
				$zip->addFile($target_path . '/assets/tgui.js', 'tgui.js');
				$zip->addFromString('gulp_output.txt', $output);
				$zip->close();
				download_file($zippath);
			}
			else
				$error = 'Unable to create output zipfile!';
			exec('rmdir "' . str_replace('/', '\\', $target_node) . '"');	//improtant
			rrmdir($target_path);
		}
		else if(!$updated_git)
			throw new RuntimeException('No valid files uploaded!');
	}
}
catch(Exception $e){
	$error = $e->getMessage();
}

?>
<!DOCTYPE html>
<html>
	<head>
		<title>TGUI .ract Compiler</title>
	</head>
	<body>
		<?php if(isset($error)) echo '<h5><font color="red">An error occured:</font> ' . $error . '</h5><br><br>'; ?>
		<h1>Upload up to <?php echo $max_number_of_uploads; ?> .ract files<h2>
		<h4>Based off revision: <?php echo isset($commit_url) ? '<a href="' . $commit_url . '">' . $revision . '</a>' : $revision; ?>
		<form id='file_form' action = '' method = 'post' enctype='multipart/form-data'>
			<input type='checkbox' name='pull'>Update to latest revision (don't use this unless you have to)<br>
			<div id='files_field'>
				<input name='f1' type='file'><br>
			</div>
			<button id='add_more'>Add Another File</button>
			<button type='submit'>Submit</button>
		</form>
	</body>
	<script type='text/javascript' src='https://code.jquery.com/jquery-3.2.1.min.js'></script>
	<script type='text/javascript'>
		$(function(){
			var next_id = 2;
			$("#add_more").click(function(e){
				$("#files_field").append("<input name='f" + next_id + "' type='file'><br>");
				++next_id;
				if(next_id > <?php echo $max_number_of_uploads; ?>)
					$("#add_more").remove();
			});
		});
	</script>
</html>
