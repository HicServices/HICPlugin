## CHI Column Finder

The CHI Column Finder is a an extraction pipeline step to find and redact potential CHIs in the extraction data.

By default it scans all cells in your export tables for a potential CHI and writes any potential CHI to a file in your extraction directory.
If any potential CHI are found, a warning message will appear at the end of your extraction.

## Using the CHI Column Finder Efficiently
Looking for CHI is an expensive process as we need to look in every cell of data in the extraction. The CHI column finder has some built-in configuration options to make this as pain-free as possible.

### Override Until
Allows for this step to be skipped until a certain point in time.

### AllowList File
The allow list provides the ability to add columns to ignore when searching for CHIs, such as UID columns and other generated fields.
It supports a global allow list across all catalogues, but also a per catalogue allow list.

A sample allow list might look like
```
RDMP_ALL:
	- UUID
	- BOOLEAN_FIELD
MY_CATALOGUE:
	- ANOTHER_UUID
```
This allow list would ignore all columns named 'UUID' and 'BOOLEAN_FIELD' in your extraction along with the column 'ANOTHER_FIELD' in the MY_CATALOGUE catalogue.


Ax example file can be found [here](./AllowList.yml).

### Bail Out After
This option allows for the extraction to stop looking for CHIs after it has found a set number (0 means it will not stop searching early). 

### Verbose Logging
Checking this option will log any potential CHI to the extraction notification window as well as the 'Found CHI' file.

