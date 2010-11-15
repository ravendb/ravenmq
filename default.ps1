properties {
  $base_dir  = resolve-path .
  $lib_dir = "$base_dir\SharedLibs"
  $build_dir = "$base_dir\build"
  $buildartifacts_dir = "$build_dir\"
  $sln_file = "$base_dir\RavenMQ.sln"
  $version = "1.0.0"
  $tools_dir = "$base_dir\Tools"
  $release_dir = "$base_dir\Release"
  $uploader = "..\Uploader\S3Uploader.exe"
}

include .\psake_ext.ps1

task default -depends OpenSource,Release

task Verify40 {
	if( (ls "$env:windir\Microsoft.NET\Framework\v4.0*") -eq $null ) {
		throw "Building Raven requires .NET 4.0, which doesn't appear to be installed on this machine"
	}
}

task Clean {
  remove-item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue
}

task Init -depends Verify40, Clean {
	
	if($env:buildlabel -eq $null) {
		$env:buildlabel = "13"
	}
	
	$projectFiles = ls -path $base_dir -include *.csproj -recurse | 
					Where { $_ -notmatch [regex]::Escape($lib_dir) } | 
					Where { $_ -notmatch [regex]::Escape($tools_dir) }
	
	foreach($projectFile in $projectFiles) {
		
		$projectDir = [System.IO.Path]::GetDirectoryName($projectFile)
		$projectName = [System.IO.Path]::GetFileName($projectDir)
		$asmInfo = [System.IO.Path]::Combine($projectDir, [System.IO.Path]::Combine("Properties", "AssemblyInfo.cs"))
		
		Generate-Assembly-Info `
			-file $asmInfo `
			-title "$projectName $version.0" `
			-description "A linq enabled document database for .NET" `
			-company "Hibernating Rhinos" `
			-product "RavenDB $version.0" `
			-version "$version.0" `
			-fileversion "1.0.0.$env:buildlabel" `
			-copyright "Copyright © Hibernating Rhinos and Ayende Rahien 2004 - 2010" `
			-clsCompliant "true"
	}
		
	new-item $release_dir -itemType directory
	new-item $buildartifacts_dir -itemType directory
	
	copy $tools_dir\xUnit\*.* $build_dir

}

task Compile -depends Init {
	$v4_net_version = (ls "$env:windir\Microsoft.NET\Framework\v4.0*").Name
  exec { &"C:\Windows\Microsoft.NET\Framework\$v4_net_version\MSBuild.exe" "$sln_file" /p:OutDir="$buildartifacts_dir\" }
}

task Test -depends Compile {
  $old = pwd
  cd $build_dir
  exec { &"$build_dir\xunit.console.clr4.exe" "$build_dir\Raven.Munin.Tests.dll" } 
  exec { &"$build_dir\xunit.console.clr4.exe" "$build_dir\Raven.MQ.Tests.dll" } 
  cd $old
}

task ReleaseNoTests -depends OpenSource,DoRelease {

}

task Commercial {
	$global:commercial = $true
	$global:uploadCategory = "RavenDB-Commercial"
}

task Unstable {
	$global:commercial = $false
	$global:uploadCategory = "RavenDB-Unstable"
}

task OpenSource {
	$global:commercial = $false
	$global:uploadCategory = "RavenDB"
}

task Release -depends Test,DoRelease { 
}


task DoRelease -depends Compile {
	
	$old = pwd
	
	if($false) {
    cd $build_dir\Output
	
	  $file = "$release_dir\$global:uploadCategory-Build-$env:buildlabel.zip"
      
    exec { 
      & $tools_dir\zip.exe -9 -A -r `
        $file `
        EmbeddedClient\*.* `
        Client\*.* `
        Samples\*.* `
        Samples\*.* `
        Client-3.5\*.* `
        Web\*.* `
        Bundles\*.* `
        Web\bin\*.* `
        Server\*.* `
        *.*
    }
	}
  cd $old
    
  ExecuteTask("ResetBuildArtifcats")
}

task ResetBuildArtifcats {
    
}

task Upload -depends DoRelease {
	Write-Host "Starting upload"
	if (Test-Path $uploader) {
		$log = $env:push_msg 
		if($log -eq $null -or $log.Length -eq 0) {
		  $log = git log -n 1 --oneline		
		}
		
		$file = "$release_dir\$global:uploadCategory-Build-$env:buildlabel.zip"
		write-host "Executing: $uploader '$global:uploadCategory' $file '$log'"
		&$uploader "$uploadCategory" $file "$log"
			
		if ($lastExitCode -ne 0) {
			write-host "Failed to upload to S3: $lastExitCode"
			throw "Error: Failed to publish build"
		}
	}
	else {
		Write-Host "could not find upload script $uploadScript, skipping upload"
	}
	
	
}

task UploadCommercial -depends Commercial, DoRelease, Upload {
		
}	

task UploadOpenSource -depends OpenSource, DoRelease, Upload {
		
}	

task UploadUnstable -depends Unstable, DoRelease, Upload {
		
}	