var table = null;
$(function () {
    table = $('#tradesTable').DataTable({
        pageLength: 100,
        responsive: true,
        colReorder: true,
        stateSave: true,
        dom: 'Bflrtip',
        buttons: [
            {
                extend: "colvis",
                text: "Columns"
            },
            "copy",
            "csv"
        ],
        order: [[0, "desc"]]
    });
});