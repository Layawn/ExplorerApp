$(document).on('click', '*[data-href]', function () {
    let action = $(location).attr('origin') + $(this).data("href").replaceAll(' ', "%20") + " .table-hover";
    $('.listview').load(action);
    setTimeout(loadDirSizes, 1000);
})

$(document).on('click', '.link', function (e) {
    e.preventDefault();
    let action = $(location).attr('origin') + $(this).attr("href").replaceAll(' ', "%20") + " .table-hover";
    $('.listview').load(action);
    setTimeout(loadDirSizes, 1000);
})

$(document).on('click', '.triangle', function (e) {
    e.preventDefault();
    let dirId = '#' + $(this).attr('name');
    let action = $(location).attr('origin') + this.getAttribute("href").replaceAll(' ', "%20") + " .myUL";
    if (!$(this).hasClass('clicked')) {
        $(dirId).load(action);
    } else {
        $(dirId).empty();
    }
    this.classList.toggle("clicked");
    this.classList.toggle("triangle-down");
})

$(document).on('click', '.table-hover th:last', function () {
    var table = $('.table-hover');
    var tbody = $('#table1');

    tbody.find('tr').sort(function (a, b) {
        if ($('#size_order').val() == 'asc') {
            return $('td:last', b).attr('title') - ($('td:last', a).attr('title'));
        }
        else {
            return $('td:last', a).attr('title') - ($('td:last', b).attr('title'));
        }

    }).appendTo(tbody);

    var sort_order = $('#size_order').val();
    if (sort_order == "asc") {
        document.getElementById("size_order").value = "desc";
        $('.table-hover th:last').addClass('sorted-desc')
        $('.table-hover th:last').removeClass('sorted-asc')
    }
    if (sort_order == "desc") {
        document.getElementById("size_order").value = "asc";
        $('.table-hover th:last').addClass('sorted-asc')
        $('.table-hover th:last').removeClass('sorted-desc')
    }
})

function loadDirSizes() {
    $('.myTD-size').each(function () {
        var url = "/Explorer/CalcDirSize";
        var folderPath = $(this).attr("path");
        var cell = $(this);
        $.post(url, { path: folderPath }, function (data) {
            $(cell).html(data + ' KB');
            $(cell).attr('title', data);
        });
    })
}

loadDirSizes();

