# FSDATAUnpacker
Unpacks and repacks various DATA.BIN archives in early FromSoftware games.  
To unpack, drag and drop a file onto the exe, or pass it's path as an argument.  
To repack, drag and drop a folder onto the exe, or pass it's path as an argument.  

When unpacking, make sure to leave the DATA part of the name alone.  
You can add extensions, but the name needs to stay the same for detection.  
Example: AC3DATA.BIN  

When repacking, make sure the folder name before any dots is the DATA part of the name.  
Example: AC3DATA  

To set the ID for a file, make sure it's name starts with that ID.  
If an ID cannot be determined from the name, the program will try to use an index instead.  
Make sure the ID is not beyond the entry count's range for the given game.  
Make sure you do not reuse the same ID, or the program will throw an error.  
The max ID is entry count - 1.  

The currently supported files and their supported entry counts are:  
ERDATA.BIN   4096  
AC2DATA.BIN  4096  
AC25DATA.BIN 8192  
AC3DATA.BIN  8192  

The games these files were found in are:  
ERDATA.BIN from Eternal Ring on PS2  
AC2DATA.BIN from Armored Core 2 on PS2  
AC25DATA.BIN from Armored Core 2: Another Age on PS2  
AC3DATA.BIN from Armored Core 3 on PS2  

The names are usually self explantory, such as AC25DATA meaning Armored Core 2.5 data.  

# Technical Notes
These archives have only been observed in little endian byte ordering.  
These archives have an entry count of usually 4096 or 8192.  
The entry count is always the same, regardless of how many files there are.  
The index a file is at in the entries is it's ID.  
You can tell from how some of them are perfectly at 100 or 200.  
Entries that are completely null are not used.  

Each entry has two 32 bit values:  
Start Sector  
Sector Count  

The start sector is the sector the file data begins at.  
The sector count is how many sectors the file data takes up, without additional sector padding.  

Each sector is 0x800 bytes in length, and they base off of where the entries end, and the data begins.  
The first file will start immediately after the entries and have a start sector of 0 for example.  

Each file aligns to 0x8000, and so does each entry count it would appear.  
I'm not sure if the entry count is perfectly 4096 or 8192.  
They could just be aligning to 0x8000 after the entries.  

The aligning to 0x8000 is not included in the sector count.  
Sector padding also ensures the total padded sector count is divisible by 16.  
If the sector count of a file is 1580, that is not divisible by 16.  
So the next file will begin at sector 1584, as observed in AC25DATA.BIN in Armored Core 2: Another Age.  
Since zero length files do not have a sector count divisible by 16, as a side affect they also have 16 sectors of padding.  

Some files in AC25DATA.BIN add an extra 16 sectors of padding even if the sector count was divisible by 16.  
I'm not sure why, as some of them also don't.