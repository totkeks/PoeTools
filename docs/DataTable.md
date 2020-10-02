# Data Table

A data table file typically uses the `.dat` file extension. It uses a table structure with columns and rows. The content is split in a fixed size and variable size data section. There is no header.

## Variations

Each data table has up to four variations, differentiated by their file extensions:

* `.dat`: 32-bit pointers, UTF-16LE strings
* `.dat64`: 64-bit pointers, UTF-16LE strings
* `.datl`: 32-bit pointers, UTF-32LE strings
* `.datl64`: 64-bit pointers, UTF-32LE strings

## Fixed size data

The fixed size data starts with the 32-bit amount of rows followed by the actual rows. The size of each row depends on the columns defined by the data table. There is no metadata for the columns in the file, thus it has to be reverse-engineered by the community.

```text
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                           Row Count                           |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
+                       Row [0] (variable)                      +
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                               ...                             |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
+                 Row [Row Count - 1] (variable)                +
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```
<!-- protocol "Row Count: 32, Row [0] (variable): 64, ...: 32, Row [Row Count - 1] (variable): 64" -->

## Variable size data

The variable size data starts with the 64-bit magic number `0xBBBB 0xBBBB 0xBBBB 0xBBBB` and is then filled with arbitrary data until the end of the file. Individual columns reference segments of that data.

```text
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
+                  0xBBBB 0xBBBB 0xBBBB 0xBBBB                  +
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
+                                                               +
|                                                               |
+                    Arbitrary Data (variable)                  +
|                                                               |
+                                                               +
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```
<!-- protocol "0xBBBB 0xBBBB 0xBBBB 0xBBBB: 64, Arbitrary Data (variable): 128" -->

## Column types

The data types used for columns are primitive types, that directly contain their values, reference types, which are pointers to actual data, or array types with multiples of the same type.

### Primitive types

| Type  | Size | Value                           |
|-------|-----:|---------------------------------|
| bool  |    8 | LSB as true or false            |
| byte  |    8 | Unsigned integer                |
| short |   16 | Signed integer                  |
| int   |   32 | Signed integer                  |
| uint  |   32 | Unsigned integer                |
| long  |   64 | Signed integer                  |
| ulong |   64 | Unsigned integer                |
| float |   32 | Single-precision floating-point |

### Reference types

#### Foreign key

A foreign key points a row in another data table using a `ulong` value for the 0-indexed row number. The target data table has to be reverse-engineered.

#### Local key

A local key points to a row in this data table using a `uint` value for the 0-indexed row number.

#### Pointer

A pointer references another type inside the variable size data section using a 32- or 64-bit offset, depending on the [variation](#Variations). The target type can be any primitive or array type.

### Array types

#### List

A list extends the preceding pointer by a 32-bit length value before the offset. The target type can be any primitive type or another pointer.

#### String

A string is always null-terminated and either UTF-16LE or UTF-32LE encoded, depending on the [variation](#Variations).
