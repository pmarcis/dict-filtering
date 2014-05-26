#!/usr/bin/perl

my $type = shift @ARGV;


$set_id = "myset";
$doc_id = "mydoc";
$sys_id = "mysys";

if ($type eq 'src') {
    $src_lang = shift @ARGV;
    print "<srcset setid=\"$set_id\" srclang=\"$src_lang\">\n";
#    print "<doc docid=\"$doc_id\" sysid=\"$sys_id\">\n";
    print "<doc docid=\"$doc_id\">\n";
}
elsif ($type eq 'ref') {
    $src_lang = shift @ARGV;
    $trg_lang = shift @ARGV;
    print "<refset setid=\"$set_id\" srclang=\"$src_lang\" trglang=\"$trg_lang\">\n";
    print "<doc docid=\"$doc_id\" sysid=\"$sys_id\">\n";
#    print "<doc docid=\"$doc_id\">\n";
}

$seg_id = 1;
while ($line = <STDIN>)
{
    $line =~ s/^\s+//;
    $line =~ s/\s+$//;
    print "<seg id=\"$seg_id\">$line</seg>\n";
    $seg_id++;
}

if ($type eq 'src') {
    print "</doc>\n";
    print "</srcset>\n";
}
elsif ($type eq 'ref') {
    print "</doc>\n";
    print "</refset>\n";
}
